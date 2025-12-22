using Drift.Cli.Abstractions;
using Drift.Cli.Tests.Utils;
using Drift.Domain;
using Drift.Domain.Device.Addresses;

namespace Drift.Cli.Tests.Commands;

internal sealed partial class ScanCommandTests {
  [Test]
  public async Task RemoteScan() {
    // Arrange
    var scanConfig = ConfigureServices(
      new CidrBlock( "192.168.0.0/24" ),
      [[new IpV4Address( "192.168.0.100" ), new MacAddress( "11:11:11:11:11:11" )]],
      new Inventory {
        Network = new Network(),
        Agents = [
          new Domain.Agent { Id = "local1", Address = "http://localhost:51515" },
          new Domain.Agent { Id = "local2", Address = "http://localhost:51516" }
        ]
      }
    );

    using var tcs = new CancellationTokenSource( TimeSpan.FromMinutes( 1 ) );

    Console.WriteLine( "Starting agents..." );
    RunningCliCommand[] agents = [
      await DriftTestCli.StartAgentAsync(
        "--adoptable -v",
        ConfigureServices(
          interfaces: new CidrBlock( "192.168.10.0/24" ),
          discoveredDevices: [
            [new IpV4Address( "192.168.10.100" ), new MacAddress( "22:22:22:22:22:22" )],
            [new IpV4Address( "192.168.10.101" ), new MacAddress( "21:21:21:21:21:21" )]
          ]
        ),
        tcs.Token
      ),
      await DriftTestCli.StartAgentAsync(
        "--adoptable -v --port 51516",
        ConfigureServices(
          interfaces: new CidrBlock( "192.168.20.0/24" ),
          discoveredDevices: [[new IpV4Address( "192.168.20.100" ), new MacAddress( "33:33:33:33:33:33" )]]
        ),
        tcs.Token
      )
    ];

    // Act
    Console.WriteLine( "Starting scan..." );
    var (scanExitCode, scanOutput, scanError) = await DriftTestCli.InvokeAsync(
      "scan unittest",
      scanConfig,
      cancellationToken: tcs.Token
    );

    Console.WriteLine( "Scan finished" );
    Console.WriteLine( "----------------" );
    Console.WriteLine( scanOutput.ToString() + scanError );
    Console.WriteLine( "----------------" );

    Console.WriteLine( "Signalling agent cancellation..." );
    await tcs.CancelAsync();
    Console.WriteLine( "Waiting for agents to shut down..." );

    foreach ( var agent in agents ) {
      var (agentExitCode, agentOutput, agentError) = await agent.Completion;

      Console.WriteLine( "Agent finished" );
      Console.WriteLine( "----------------" );
      Console.WriteLine( agentOutput.ToString() + agentError );
      Console.WriteLine( "----------------" );

      Assert.That( agentExitCode, Is.EqualTo( ExitCodes.Success ) );
    }

    // Assert
    Assert.That( scanExitCode, Is.EqualTo( ExitCodes.Success ) );
    await Verify( scanOutput.ToString() + scanError );
  }
}