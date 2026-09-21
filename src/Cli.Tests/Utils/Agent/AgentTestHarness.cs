using System.Collections.Immutable;
using System.Net.NetworkInformation;
using System.Text.RegularExpressions;
using Drift.Cli.Abstractions;
using Drift.Cli.Settings.Serialization;
using Drift.Cli.Settings.Tests;
using Drift.Cli.Tests.Utils.Coordinator;
using Drift.Cli.Tests.Utils.Network.Topology;
using Drift.Cli.Tests.Utils.Testing;
using Drift.Common.IO;
using Drift.Coordinator.Services.State;
using Drift.Domain;
using Drift.Domain.Device.Addresses;
using Drift.Domain.Device.Discovered;
using Drift.Domain.Scan;
using Drift.Scanning.Scanners.Factories;
using Drift.Scanning.Subnets.Interface;
using Drift.Spec.Serialization;
using Drift.TestUtilities.IO;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using DomainAgent = Drift.Domain.Agent;
using DomainNetwork = Drift.Domain.Network;
using NetworkInterface = Drift.Scanning.Subnets.Interface.NetworkInterface;

namespace Drift.Cli.Tests.Utils.Agent;

/// <summary>
/// Runs a multi-agent scan through a coordinator, invoking it through the CLI.
/// </summary>
internal sealed class AgentTestHarness : IAsyncDisposable {
  private readonly NetworkTopology _topology;
  private readonly List<AgentConfiguration> _agentConfigs;
  private readonly CoordinatorConfiguration _coordinatorConfig;
  private readonly CancellationTokenSource _cancellationTokenSource;
  private readonly ISettingsLocation _settingsLocation;
  private readonly IDriftDataLocation _driftDataLocation;
  private readonly ICoordinatorDataLocation _dataLocation;
  private readonly string _specFilePath;
  private readonly ushort _controlPort;
  private readonly Uri _serverAddress;
  private readonly List<(AgentConfiguration Configuration, RunningCliCommand Command)> _runningAgents = [];
  private RunningCliCommand? _runningServer;

  private AgentTestHarness(
    NetworkTopology topology,
    List<AgentConfiguration> agentConfigs,
    CoordinatorConfiguration coordinatorConfig,
    TemporaryCoordinatorDataLocation dataLocation,
    ISettingsLocation settingsLocation,
    IDriftDataLocation driftDataLocation,
    TimeSpan timeout
  ) {
    _topology = topology;
    _agentConfigs = agentConfigs;
    _coordinatorConfig = coordinatorConfig;
    _controlPort = TcpUtils.GetFreePort();
    _dataLocation = dataLocation;
    _settingsLocation = settingsLocation;
    _driftDataLocation = driftDataLocation;
    _specFilePath = Path.Combine( dataLocation.Directory, "test.spec.yaml" );
    _serverAddress = new Uri( $"http://127.0.0.1:{_controlPort}" );
    _cancellationTokenSource = new CancellationTokenSource( timeout );
  }

  /// <summary>
  /// Creates a harness from a network topology.
  /// </summary>
  public static async Task<AgentTestHarness> CreateAsync(
    NetworkTopology topology,
    TimeSpan? timeout = null
  ) {
    var agentConfigs = NetworkTopologyAdapter.ToAgentConfigurations( topology )
      .Select( agent => agent with { Address = new Uri( $"http://127.0.0.1:{TcpUtils.GetFreePort()}" ) } )
      .ToList();
    var coordinatorConfig = NetworkTopologyAdapter.ToCoordinatorConfiguration( topology );

    var harness = new AgentTestHarness(
      topology,
      agentConfigs,
      coordinatorConfig,
      new TemporaryCoordinatorDataLocation(),
      new TemporarySettingsLocation(),
      new TemporaryDriftDataLocation(),
      timeout ?? TimeSpan.FromMinutes( 1 )
    );

    Validate( agentConfigs, coordinatorConfig );

    try {
      await harness.StartAgentsAsync();
      await harness.StartServerAsync();
      await harness.ConfigureCoordinatorAsync();
      return harness;
    }
    catch {
      await harness.DisposeAsync();
      throw;
    }
  }

  private static void Validate(
    List<AgentConfiguration> agentConfigs,
    CoordinatorConfiguration coordinatorConfig
  ) {
    var blankAgents = agentConfigs.Where( agent => string.IsNullOrWhiteSpace( agent.Id.Value ) ).ToList();
    if ( blankAgents.Count > 0 ) {
      throw new InvalidOperationException( "One or more agents have a blank ID." );
    }

    var duplicateIds = agentConfigs
      .GroupBy( agent => agent.Id )
      .Where( group => group.Count() > 1 )
      .Select( group => group.Key )
      .ToList();

    if ( duplicateIds.Count > 0 ) {
      throw new InvalidOperationException(
        $"Agent IDs must be unique. Duplicates: {string.Join( ", ", duplicateIds )}"
      );
    }

    foreach ( var agent in agentConfigs ) {
      var extraKeys = agent.DiscoveredDevices.Keys.Except( agent.VisibleSubnets ).ToList();
      if ( extraKeys.Count > 0 ) {
        throw new InvalidOperationException(
          $"Agent {agent.Id} has devices in subnets it cannot see: {string.Join( ", ", extraKeys )}"
        );
      }

      foreach ( var (cidr, devices) in agent.DiscoveredDevices ) {
        var outOfRange = devices
          .Where( device => device.Ip is not null && !cidr.Contains( device.Ip.Value ) )
          .Select( device => device.Ip!.Value.Value )
          .ToList();
        if ( outOfRange.Count > 0 ) {
          throw new InvalidOperationException(
            $"Agent {agent.Id} has devices in subnet {cidr} whose IPs are outside that subnet: {string.Join( ", ", outOfRange )}"
          );
        }
      }
    }

    var coordinatorExtraKeys = coordinatorConfig.DiscoveredDevices.Keys
      .Except( coordinatorConfig.VisibleSubnets )
      .ToList();
    if ( coordinatorExtraKeys.Count > 0 ) {
      throw new InvalidOperationException(
        $"Coordinator has devices in subnets it cannot see: {string.Join( ", ", coordinatorExtraKeys )}"
      );
    }
  }

  private async Task StartAgentsAsync() {
    foreach ( var agentConfig in _agentConfigs ) {
      var additionalArgs = string.IsNullOrWhiteSpace( agentConfig.AdditionalArgs )
        ? string.Empty
        : $" {agentConfig.AdditionalArgs}";
      var agent = await DriftTestCli.StartAgentAsync(
        $"--port {agentConfig.Address.Port}{additionalArgs}",
        _cancellationTokenSource.Token,
        BuildAgentConfiguration( agentConfig )
      );
      _runningAgents.Add( ( agentConfig, agent ) );
    }
  }

  private async Task StartServerAsync() {
    _runningServer = await DriftTestCli.StartServerAsync(
      $"--port {_controlPort} --no-agent",
      _cancellationTokenSource.Token,
      BuildServerConfiguration()
    );
  }

  public async Task<HarnessResult> RunCliAsync( string arguments ) {
    var result = await InvokeCliAsync( arguments );

    return new HarnessResult {
      ScanExitCode = result.ExitCode,
      ScanOutput = NormalizeOutput( result.Output.ToString() ?? string.Empty ),
      ScanError = NormalizeOutput( result.Error.ToString() ?? string.Empty ),
      EnrolledAgentIds = _agentConfigs.Select( agent => agent.Id ).ToArray()
    };
  }

  private async Task RunCliRequiredAsync( string arguments ) {
    var result = await InvokeCliAsync( arguments );

    if ( result.ExitCode == ExitCodes.Success ) {
      return;
    }

    throw new InvalidOperationException(
      $"Unable to '{arguments}'. Output:\n{result.Output}\nError:\n{result.Error}"
    );
  }

  private Task<CliCommandResult> InvokeCliAsync( string arguments ) {
    return DriftTestCli.InvokeAsync(
      arguments,
      configureServices: ConfigureApplicationDataLocation,
      cancellationToken: _cancellationTokenSource.Token,
      settingsLocation: _settingsLocation
    );
  }

  private void ConfigureApplicationDataLocation( IServiceCollection services ) {
    services.Replace( ServiceDescriptor.Singleton<IDriftDataLocation>( _ => _driftDataLocation ) );
  }

  private async Task ConfigureCoordinatorAsync() {
    await RunCliRequiredAsync( $"env add test-server {_serverAddress}" );
    await File.WriteAllTextAsync(
      _specFilePath,
      YamlConverter.Serialize( BuildInventory() ),
      _cancellationTokenSource.Token
    );
    await RunCliRequiredAsync( $"spec apply \"{_specFilePath}\"" );

    foreach ( var agent in _agentConfigs ) {
      await RunCliRequiredAsync( $"enrollment add {agent.Id}" );
    }
  }

  private Inventory BuildInventory() {
    return new Inventory {
      Network = new DomainNetwork {
        Subnets = _topology.Subnets
          .Select( subnet => new DeclaredSubnet { Id = subnet.Name, Address = subnet.Cidr.ToString() } )
          .ToList()
      },
      Agents = _agentConfigs
        .Select( agent => new DomainAgent { Id = agent.Id, Address = agent.Address.ToString() } )
        .ToList()
    };
  }

  private Action<IServiceCollection> BuildServerConfiguration() {
    return services => {
      services.Replace( ServiceDescriptor.Singleton<ICoordinatorDataLocation>( _ => _dataLocation ) );
      services.Replace( ServiceDescriptor.Singleton<IDriftDataLocation>( _ => _driftDataLocation ) );
      services.Replace( ServiceDescriptor.Scoped<IInterfaceSubnetProvider>( _ =>
          new PredefinedInterfaceSubnetProvider( BuildInterfaces( _coordinatorConfig.VisibleSubnets ) )
        )
      );
      services.Replace( ServiceDescriptor.Scoped<ISubnetScannerFactory>( _ =>
          new MockSubnetScannerFactory( BuildSubnetResults( _coordinatorConfig.DiscoveredDevices ) )
        )
      );
    };
  }

  private static Action<IServiceCollection> BuildAgentConfiguration( AgentConfiguration agentConfig ) {
    return services => {
      services.Replace( ServiceDescriptor.Scoped<IInterfaceSubnetProvider>( _ =>
          new PredefinedInterfaceSubnetProvider( BuildInterfaces( agentConfig.VisibleSubnets ) )
        )
      );
      services.Replace( ServiceDescriptor.Scoped<ISubnetScannerFactory>( _ =>
          new MockSubnetScannerFactory( BuildSubnetResults( agentConfig.DiscoveredDevices ) )
        )
      );
    };
  }

  private static List<INetworkInterface> BuildInterfaces( IEnumerable<CidrBlock> subnets ) {
    return subnets
      .Select( cidr => new NetworkInterface {
        Description = $"eth_{cidr}", OperationalStatus = OperationalStatus.Up, UnicastAddress = cidr
      } )
      .ToList<INetworkInterface>();
  }

  private static Dictionary<CidrBlock, SubnetScanResult> BuildSubnetResults(
    IReadOnlyDictionary<CidrBlock, List<DeviceAddressSet>> devicesBySubnet
  ) {
    return devicesBySubnet.ToDictionary(
      pair => pair.Key,
      pair => {
        var devices = pair.Value
          .Select( device => new DiscoveredDevice { Addresses = device.ToAddresses(), Timestamp = DateTime.UtcNow } )
          .ToList();
        return new SubnetScanResult {
          CidrBlock = pair.Key,
          DiscoveredDevices = devices,
          Metadata = new Metadata { StartedAt = DateTime.UtcNow, EndedAt = DateTime.UtcNow },
          Status = ScanResultStatus.Success,
          DiscoveryAttempts = devices
            .SelectMany( device => device.Addresses.OfType<IpV4Address>() )
            .ToImmutableHashSet()
        };
      }
    );
  }

  private static string NormalizeOutput( string output ) {
    return Regex.Replace(
      Regex.Replace( output, @"[ \t]+(?=\r?$)", string.Empty, RegexOptions.Multiline ),
      "Coordinator scan [0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}",
      "Coordinator scan <scan-id>",
      RegexOptions.IgnoreCase
    );
  }

  public async ValueTask DisposeAsync() {
    await _cancellationTokenSource.CancelAsync();

    if ( _runningServer is not null ) {
      await _runningServer.DisposeAsync();
    }

    foreach ( var (_, agent) in _runningAgents ) {
      await agent.DisposeAsync();
    }

    _cancellationTokenSource.Dispose();
    DeleteDirectory( _dataLocation.Directory );
    DeleteDirectory( _settingsLocation.Directory );
    DeleteDirectory( _driftDataLocation.Directory );
  }

  private static void DeleteDirectory( string directory ) {
    if ( Directory.Exists( directory ) ) {
      Directory.Delete( directory, recursive: true );
    }
  }
}