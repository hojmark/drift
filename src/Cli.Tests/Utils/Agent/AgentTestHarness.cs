using System.Collections.Immutable;
using System.Net.NetworkInformation;
using Drift.Cli.Abstractions;
using Drift.Cli.SpecFile;
using Drift.Cli.Tests.Utils.Network.Topology;
using Drift.Cli.Tests.Utils.Testing;
using Drift.Domain;
using Drift.Domain.Device.Addresses;
using Drift.Domain.Device.Discovered;
using Drift.Domain.Scan;
using Drift.Scanning.Scanners;
using Drift.Scanning.Subnets.Interface;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using DomainNetwork = Drift.Domain.Network;
using NetworkInterface = Drift.Scanning.Subnets.Interface.NetworkInterface;

namespace Drift.Cli.Tests.Utils.Agent;

/// <summary>
/// Test harness for orchestrating distributed scans with multiple agents.
/// </summary>
public sealed class AgentTestHarness : IAsyncDisposable {
  private const string SpecName = "unittest";

  // Thread-safe port allocation for parallel test execution
  // Note that this does not guarantee that the port is free...
  private static int _nextPort = Ports.AgentDefault;

  private readonly List<AgentConfiguration> _agents;
  private readonly CliConfiguration _cli;
  private readonly List<RunningCliCommand> _runningAgents = [];
  private readonly CancellationTokenSource _cancellationTokenSource;

  private AgentTestHarness(
    List<AgentConfiguration> agents,
    CliConfiguration cli,
    TimeSpan timeout
  ) {
    _agents = agents;
    _cli = cli;
    _cancellationTokenSource = new CancellationTokenSource( timeout );
  }

  /// <summary>
  /// Creates a harness from a <see cref="NetworkTopology"/>.
  /// </summary>
  public static async Task<AgentTestHarness> CreateAsync(
    NetworkTopology topology,
    TimeSpan? timeout = null
  ) {
    var agents = NetworkTopologyAdapter.ToAgentConfigurations( topology )
      .Select( a => a with { Address = new Uri( $"http://localhost:{Interlocked.Add( ref _nextPort, 1 )}" ) } )
      .ToList();
    var cliConfig = NetworkTopologyAdapter.ToCliConfiguration( topology );

    var harness = new AgentTestHarness(
      agents,
      cliConfig,
      timeout ?? TimeSpan.FromMinutes( 1 )
    );

    Validate( agents, cliConfig );

    await harness.StartAgentsAsync();

    return harness;
  }

  private static void Validate( List<AgentConfiguration> agents, CliConfiguration cliConfig ) {
    // Agent IDs must be non-empty and unique
    var blankAgents = agents.Where( a => string.IsNullOrWhiteSpace( a.Id.Value ) ).ToList();
    if ( blankAgents.Count > 0 ) {
      throw new InvalidOperationException( "One or more agents have a blank ID." );
    }

    var duplicateIds = agents
      .GroupBy( a => a.Id )
      .Where( g => g.Count() > 1 )
      .Select( g => g.Key )
      .ToList();
    if ( duplicateIds.Count > 0 ) {
      throw new InvalidOperationException(
        $"Agent IDs must be unique. Duplicates: {string.Join( ", ", duplicateIds )}" );
    }

    foreach ( var agent in agents ) {
      // Device subnets must be in VisibleSubnets
      var extraKeys = agent.DiscoveredDevices.Keys
        .Except( agent.VisibleSubnets )
        .ToList();
      if ( extraKeys.Count > 0 ) {
        throw new InvalidOperationException(
          $"Agent {agent.Id} has devices in subnets it cannot see: {string.Join( ", ", extraKeys )}" );
      }

      // Device IPs must fall within their declared subnet
      foreach ( var (cidr, devices) in agent.DiscoveredDevices ) {
        var outOfRange = devices
          .Where( d => d.Ip is not null && !cidr.Contains( d.Ip.Value ) )
          .Select( d => d.Ip!.Value.Value )
          .ToList();
        if ( outOfRange.Count > 0 ) {
          throw new InvalidOperationException(
            $"Agent {agent.Id} has devices in subnet {cidr} whose IPs are outside that subnet: {string.Join( ", ", outOfRange )}" );
        }
      }
    }

    var cliExtraKeys = cliConfig.DiscoveredDevices.Keys
      .Except( cliConfig.VisibleSubnets )
      .ToList();
    if ( cliExtraKeys.Count > 0 ) {
      throw new InvalidOperationException(
        $"CLI has devices in subnets it cannot see: {string.Join( ", ", cliExtraKeys )}" );
    }

    // CLI device IPs must fall within their declared subnet
    foreach ( var (cidr, devices) in cliConfig.DiscoveredDevices ) {
      var outOfRange = devices
        .Where( d => d.Ip is not null && !cidr.Contains( d.Ip.Value ) )
        .Select( d => d.Ip!.Value.Value )
        .ToList();
      if ( outOfRange.Count > 0 ) {
        throw new InvalidOperationException(
          $"CLI has devices in subnet {cidr} whose IPs are outside that subnet: {string.Join( ", ", outOfRange )}" );
      }
    }

    if ( cliConfig.ReachableAgents != null ) {
      var knownIds = agents.Select( a => a.Id ).ToHashSet();
      var unknownIds = cliConfig.ReachableAgents
        .Except( knownIds )
        .ToList();
      if ( unknownIds.Count > 0 ) {
        throw new InvalidOperationException(
          $"CLI.ReachableAgentIds references unknown agent IDs: {string.Join( ", ", unknownIds )}" );
      }
    }
  }

  /// <summary>
  /// Runs a scan using the configured agents.
  /// </summary>
  public async Task<HarnessResult> RunScanAsync() {
    Console.WriteLine( "Starting scan..." );

    var scanConfig = BuildScanConfiguration();

    var (scanExitCode, scanOutput, scanError) = await DriftTestCli.InvokeAsync(
      "scan " + SpecName,
      scanConfig,
      cancellationToken: _cancellationTokenSource.Token
    );

    Console.WriteLine( "\nScan finished" );
    Console.WriteLine( "----------------" );
    Console.WriteLine( scanOutput.ToString() + scanError );
    Console.WriteLine( "----------------\n" );

    // Stop agents and collect their exit codes
    Console.WriteLine( "Signalling agent cancellation..." );
    await _cancellationTokenSource.CancelAsync();
    Console.WriteLine( "Waiting for agents to shut down..." );

    var agentExitCodes = new Dictionary<AgentId, int>();
    for ( int i = 0; i < _runningAgents.Count; i++ ) {
      var agent = _runningAgents[i];
      var agentId = _agents[i].Id;

      var (agentExitCode, agentOutput, agentError) = await agent.Completion;

      Console.WriteLine( $"\nAgent {agentId} finished" );
      Console.WriteLine( "----------------" );
      Console.WriteLine( agentOutput.ToString() + agentError );
      Console.WriteLine( "----------------\n" );

      agentExitCodes[agentId] = agentExitCode;
    }

    return new HarnessResult {
      ScanExitCode = scanExitCode,
      ScanOutput = scanOutput.ToString() ?? string.Empty,
      ScanError = scanError.ToString() ?? string.Empty,
      ScanResult = null, // TODO: Capture actual result
      AgentExitCodes = agentExitCodes
    };
  }

  private async Task StartAgentsAsync() {
    Console.WriteLine( $"Starting {_agents.Count} agent(s)..." );

    foreach ( var agentConfig in _agents ) {
      Console.WriteLine( $"Starting agent {agentConfig.Id} on port {agentConfig.Address.Port}..." );

      var args = $"{agentConfig.AdditionalArgs} --port {agentConfig.Address.Port}";

      var agent = await DriftTestCli.StartAgentAsync(
        args,
        _cancellationTokenSource.Token,
        BuildAgentConfiguration( agentConfig )
      );

      _runningAgents.Add( agent );

      Console.WriteLine( $"Agent {agentConfig.Id} started successfully" );
    }
  }

  private static Action<IServiceCollection> BuildAgentConfiguration( AgentConfiguration agentConfig ) {
    return services => {
      // Configure interfaces the agent can see
      var interfaces = agentConfig.VisibleSubnets
        .Select( cidr => new NetworkInterface {
          Description = $"eth_{cidr}", OperationalStatus = OperationalStatus.Up, UnicastAddress = cidr
        } )
        .ToList<INetworkInterface>();

      services.Replace( ServiceDescriptor.Scoped<IInterfaceSubnetProvider>( _ =>
          new PredefinedInterfaceSubnetProvider( interfaces )
        )
      );

      // Configure subnet scanner factory with mock results
      var resultsByCidr = new Dictionary<CidrBlock, SubnetScanResult>();

      foreach ( var (cidr, deviceAddressList) in agentConfig.DiscoveredDevices ) {
        var discoveredDevices = deviceAddressList
          .Select( d => new DiscoveredDevice { Addresses = d.ToAddresses(), Timestamp = DateTime.UtcNow } )
          .ToList();

        var ipAddresses = discoveredDevices
          .SelectMany( d => d.Addresses.OfType<IpV4Address>() )
          .ToImmutableHashSet();

        resultsByCidr[cidr] = new SubnetScanResult {
          CidrBlock = cidr,
          DiscoveredDevices = discoveredDevices,
          Metadata = new Metadata { StartedAt = DateTime.UtcNow, EndedAt = DateTime.UtcNow },
          Status = ScanResultStatus.Success,
          DiscoveryAttempts = ipAddresses
        };
      }

      services.Replace( ServiceDescriptor.Scoped<ISubnetScannerFactory>( _ =>
          new MockSubnetScannerFactory( resultsByCidr )
        )
      );
    };
  }

  private Action<IServiceCollection> BuildScanConfiguration() {
    return services => {
      // Configure CLI's local interfaces
      var cliInterfaces = _cli.VisibleSubnets
        .Select( cidr => new NetworkInterface {
          Description = $"cli_eth_{cidr}", OperationalStatus = OperationalStatus.Up, UnicastAddress = cidr
        } )
        .ToList<INetworkInterface>();

      services.Replace( ServiceDescriptor.Scoped<IInterfaceSubnetProvider>( _ =>
          new PredefinedInterfaceSubnetProvider( cliInterfaces )
        )
      );

      // Configure subnet scanner factory with mock results for CLI's local scans
      var resultsByCidr = new Dictionary<CidrBlock, SubnetScanResult>();

      foreach ( var (cidr, deviceAddressList) in _cli.DiscoveredDevices ) {
        var discoveredDevices = deviceAddressList
          .Select( d => new DiscoveredDevice { Addresses = d.ToAddresses(), Timestamp = DateTime.UtcNow } )
          .ToList();

        var ipAddresses = discoveredDevices
          .SelectMany( d => d.Addresses.OfType<IpV4Address>() )
          .ToImmutableHashSet();

        resultsByCidr[cidr] = new SubnetScanResult {
          CidrBlock = cidr,
          DiscoveredDevices = discoveredDevices,
          Metadata = new Metadata { StartedAt = DateTime.UtcNow, EndedAt = DateTime.UtcNow },
          Status = ScanResultStatus.Success,
          DiscoveryAttempts = ipAddresses
        };
      }

      services.Replace( ServiceDescriptor.Scoped<ISubnetScannerFactory>( _ =>
          new MockSubnetScannerFactory( resultsByCidr )
        )
      );

      // Configure inventory with only the agents CLI can reach.
      // If ReachableAgentIds is null, all agents are reachable (no CLI subnet or no firewall).
      var reachableAgents = _cli.ReachableAgents == null
        ? _agents
        : _agents.Where( a => _cli.ReachableAgents.Contains( a.Id ) ).ToList();

      var inventory = new Inventory {
        Network = _cli.Network ?? new DomainNetwork(),
        Agents = reachableAgents
          .Select( a => new Domain.Agent { Id = a.Id, Address = a.Address.ToString() } )
          .ToList()
      };

      services.AddScoped<ISpecFileProvider>( _ =>
        new PredefinedSpecProvider( new Dictionary<string, Inventory> { { SpecName, inventory } } )
      );
    };
  }

  public async ValueTask DisposeAsync() {
    await _cancellationTokenSource.CancelAsync();

    foreach ( var agent in _runningAgents ) {
      try {
        await agent.Completion;
      }
      catch {
        // Ignore errors during disposal
      }
    }

    _cancellationTokenSource.Dispose();
  }
}