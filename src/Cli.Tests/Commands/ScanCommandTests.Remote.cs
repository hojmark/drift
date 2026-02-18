using Drift.Cli.Abstractions;
using Drift.Cli.Tests.Utils;
using Drift.Domain;
using Drift.Domain.Device.Addresses;
using Drift.Domain.Scan;
using Drift.Scanning.Scanners;
using Microsoft.Extensions.Logging;

namespace Drift.Cli.Tests.Commands;

internal sealed partial class ScanCommandTests {
  /// <summary>
  /// Mock subnet scanner that returns predefined results for testing
  /// </summary>
  private sealed class MockSubnetScanner( SubnetScanResult result ) : ISubnetScanner {
    public event EventHandler<SubnetScanResult>? ResultUpdated;

    public Task<SubnetScanResult> ScanAsync(
      SubnetScanOptions options,
      ILogger logger,
      CancellationToken cancellationToken = default
    ) {
      // Simulate progress updates
      var progressResult = new SubnetScanResult {
        CidrBlock = result.CidrBlock,
        DiscoveredDevices = result.DiscoveredDevices,
        Metadata = result.Metadata,
        Status = result.Status,
        DiscoveryAttempts = result.DiscoveryAttempts,
        Progress = new Percentage( 50 )
      };
      ResultUpdated?.Invoke( this, progressResult );

      var finalResult = new SubnetScanResult {
        CidrBlock = result.CidrBlock,
        DiscoveredDevices = result.DiscoveredDevices,
        Metadata = result.Metadata,
        Status = result.Status,
        DiscoveryAttempts = result.DiscoveryAttempts,
        Progress = new Percentage( 100 )
      };
      ResultUpdated?.Invoke( this, finalResult );

      return Task.FromResult( finalResult );
    }
  }

  /// <summary>
  /// Mock factory that creates scanners with predefined results based on CIDR
  /// </summary>
  private sealed class MockSubnetScannerFactory(
    Dictionary<CidrBlock, SubnetScanResult> resultsByCidr
  ) : ISubnetScannerFactory {
    public ISubnetScanner Get( CidrBlock cidr ) {
      if ( resultsByCidr.TryGetValue( cidr, out var result ) ) {
        return new MockSubnetScanner( result );
      }

      // Return empty result for unknown CIDRs
      return new MockSubnetScanner( new SubnetScanResult {
        CidrBlock = cidr,
        DiscoveredDevices = [],
        Metadata = new Metadata { StartedAt = default, EndedAt = default },
        Status = ScanResultStatus.Success,
        DiscoveryAttempts = System.Collections.Immutable.ImmutableHashSet<IpV4Address>.Empty
      } );
    }
  }
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
        "--adoptable",
        tcs.Token,
        ConfigureServices(
          interfaces: new CidrBlock( "192.168.10.0/24" ),
          discoveredDevices: [
            [new IpV4Address( "192.168.10.100" ), new MacAddress( "22:22:22:22:22:22" )],
            [new IpV4Address( "192.168.10.101" ), new MacAddress( "21:21:21:21:21:21" )]
          ]
        )
      ),
      await DriftTestCli.StartAgentAsync(
        "--adoptable --port 51516",
        tcs.Token,
        ConfigureServices(
          interfaces: new CidrBlock( "192.168.20.0/24" ),
          discoveredDevices: [[new IpV4Address( "192.168.20.100" ), new MacAddress( "33:33:33:33:33:33" )]]
        )
      )
    ];

    // Act
    Console.WriteLine( "Starting scan..." );
    var (scanExitCode, scanOutput, scanError) = await DriftTestCli.InvokeAsync(
      "scan unittest",
      scanConfig,
      cancellationToken: tcs.Token
    );

    Console.WriteLine( "\nScan finished" );
    Console.WriteLine( "----------------" );
    Console.WriteLine( scanOutput.ToString() + scanError );
    Console.WriteLine( "----------------\n" );

    Console.WriteLine( "Signalling agent cancellation..." );
    await tcs.CancelAsync();
    Console.WriteLine( "Waiting for agents to shut down..." );

    foreach ( var agent in agents ) {
      var (agentExitCode, agentOutput, agentError) = await agent.Completion;

      Console.WriteLine( "\nAgent finished" );
      Console.WriteLine( "----------------" );
      Console.WriteLine( agentOutput.ToString() + agentError );
      Console.WriteLine( "----------------\n" );

      Assert.That( agentExitCode, Is.EqualTo( ExitCodes.Success ) );
    }

    // Assert
    Assert.That( scanExitCode, Is.EqualTo( ExitCodes.Success ) );
    await Verify( scanOutput.ToString() + scanError );
  }
}