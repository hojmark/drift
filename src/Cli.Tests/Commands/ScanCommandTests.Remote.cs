using Drift.Cli.Abstractions;
using Drift.Cli.Tests.Utils;
using Drift.Domain;
using Drift.Domain.Device.Addresses;
using Drift.Domain.Scan;
using Drift.Scanning.Scanners;
using Microsoft.Extensions.Logging;

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
          new Domain.Agent { Id = "agentid_local1", Address = "http://localhost:51515" },
          new Domain.Agent { Id = "agentid_local2", Address = "http://localhost:51516" }
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

  [Test]
  public async Task RemoteScan_OverlappingSubnets() {
    // Arrange - Both agents can see the same subnet (192.168.10.0/24)
    var scanConfig = ConfigureServices(
      new CidrBlock( "192.168.0.0/24" ),
      [[new IpV4Address( "192.168.0.100" ), new MacAddress( "11:11:11:11:11:11" )]],
      new Inventory {
        Network = new Network(),
        Agents = [
          new Domain.Agent { Id = "agentid_local1", Address = "http://localhost:51515" },
          new Domain.Agent { Id = "agentid_local2", Address = "http://localhost:51516" }
        ]
      }
    );

    using var tcs = new CancellationTokenSource( TimeSpan.FromMinutes( 1 ) );

    Console.WriteLine( "Starting agents with overlapping subnet visibility..." );
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
          interfaces: new CidrBlock( "192.168.10.0/24" ), // Same subnet as agent1
          discoveredDevices: [
            [new IpV4Address( "192.168.10.102" ), new MacAddress( "44:44:44:44:44:44" )],
            [new IpV4Address( "192.168.10.103" ), new MacAddress( "55:55:55:55:55:55" )]
          ]
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

    // Verify the subnet was only scanned once (not twice)
    var outputStr = scanOutput.ToString();
    Console.WriteLine( "Checking output for duplicate scans..." );

    // The output should show "192.168.10.0/24" being scanned, but only once
    // We expect to see results from only ONE agent (the first one to claim it)
    await Verify( outputStr + scanError );
  }

  [Test]
  public async Task RemoteScan_EmptyResults() {
    // Arrange - Agents report subnets but no devices are found
    var scanConfig = ConfigureServices(
      new CidrBlock( "192.168.0.0/24" ),
      [], // No local devices
      new Inventory {
        Network = new Network(),
        Agents = [
          new Domain.Agent { Id = "agentid_local1", Address = "http://localhost:51515" }
        ]
      }
    );

    using var tcs = new CancellationTokenSource( TimeSpan.FromMinutes( 1 ) );

    Console.WriteLine( "Starting agent with empty scan results..." );
    RunningCliCommand[] agents = [
      await DriftTestCli.StartAgentAsync(
        "--adoptable",
        tcs.Token,
        ConfigureServices(
          interfaces: new CidrBlock( "192.168.10.0/24" ),
          discoveredDevices: [] // No devices found
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
      var (agentExitCode, _, _) = await agent.Completion;
      Assert.That( agentExitCode, Is.EqualTo( ExitCodes.Success ) );
    }

    // Assert
    Assert.That( scanExitCode, Is.EqualTo( ExitCodes.Success ) );
    await Verify( scanOutput.ToString() + scanError );
  }

  [Test]
  public async Task RemoteScan_AgentsOnly_NoLocalInterfaces() {
    // Arrange - CLI has no local interfaces, all scanning delegated to agents
    var scanConfig = ConfigureServices(
      interfaces: (CidrBlock?) null, // No local interfaces - explicitly cast to resolve ambiguity
      discoveredDevices: new List<List<IDeviceAddress>>(),
      inventory: new Inventory {
        Network = new Network(),
        Agents = [
          new Domain.Agent { Id = "agentid_local1", Address = "http://localhost:51515" },
          new Domain.Agent { Id = "agentid_local2", Address = "http://localhost:51516" }
        ]
      }
    );

    using var tcs = new CancellationTokenSource( TimeSpan.FromMinutes( 1 ) );

    Console.WriteLine( "Starting agents for agents-only scan..." );
    RunningCliCommand[] agents = [
      await DriftTestCli.StartAgentAsync(
        "--adoptable",
        tcs.Token,
        ConfigureServices(
          interfaces: new CidrBlock( "192.168.10.0/24" ),
          discoveredDevices: [
            [new IpV4Address( "192.168.10.100" ), new MacAddress( "22:22:22:22:22:22" )]
          ]
        )
      ),
      await DriftTestCli.StartAgentAsync(
        "--adoptable --port 51516",
        tcs.Token,
        ConfigureServices(
          interfaces: new CidrBlock( "192.168.20.0/24" ),
          discoveredDevices: [
            [new IpV4Address( "192.168.20.100" ), new MacAddress( "33:33:33:33:33:33" )]
          ]
        )
      )
    ];

    // Act
    Console.WriteLine( "Starting scan with no local interfaces..." );
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
      var (agentExitCode, _, _) = await agent.Completion;
      Assert.That( agentExitCode, Is.EqualTo( ExitCodes.Success ) );
    }

    // Assert
    Assert.That( scanExitCode, Is.EqualTo( ExitCodes.Success ) );
    await Verify( scanOutput.ToString() + scanError );
  }

  /// <summary>
  /// Mock subnet scanner that returns predefined results for testing.
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
  /// Mock factory that creates scanners with predefined results based on CIDR.
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
}