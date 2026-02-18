using System.CommandLine;
using Drift.Cli.Abstractions;
using Drift.Cli.Commands.Common.Commands;
using Drift.Cli.Commands.Scan.Interactive;
using Drift.Cli.Commands.Scan.Interactive.Input;
using Drift.Cli.Commands.Scan.NonInteractive;
using Drift.Cli.Presentation.Console.Logging;
using Drift.Cli.Presentation.Console.Managers.Abstractions;
using Drift.Cli.SpecFile;
using Drift.Common.Network;
using Drift.Domain;
using Drift.Domain.Scan;
using Drift.Networking.Cluster;
using Drift.Networking.PeerStreaming.Core.Abstractions;
using Drift.Scanning.Subnets;
using Drift.Scanning.Subnets.Interface;

namespace Drift.Cli.Commands.Scan;

/*
 *   Monitor mode:
 *     drift monitor declared.yaml --interval 10m --notify slack,email,log,webhook
 */
internal class ScanCommand : CommandBase<ScanParameters, ScanCommandHandler> {
  public ScanCommand( IServiceProvider provider ) : base( "scan", "Scan the network and detect drift", provider ) {
    Add( ScanParameters.Options.Interactive );
  }

  /* private enum ShowMode {
     All = 1,
     Changed = 2,
     Unchanged = 3
   }*/

  // var monitorOption = new Option<bool>( "--monitor", "Continually scan network(s) until manually stopped." );
  // AddOption( monitorOption );
  // var monitorIntervalOption = new Option<TimeSpan>( "--interval", "Scan interval when in monitor mode." );
  // AddOption( monitorIntervalOption );
  // var monitorNotifyOption = new Option<string>( "--notify", "Notification channels when in monitor mode." );
  // "save" or "update" instead?
  // AddOption( new Option<bool>( "--write", "Create or update reference with discovered resources (devices, subnets etc.)." ) );
  // Combine with option to change the default
  // Alternative: --changed [all|skip|only]
  /*var changed = new Option<ShowMode>(
      "--show",
      () => ShowMode.All,
      "Select which devices to show: all (show all), changed (only changed devices), unchanged (only unchanged devices)"
    );*/

  protected override ScanParameters CreateParameters( ParseResult result ) {
    return new ScanParameters( result );
  }
}

internal class ScanCommandHandler(
  IOutputManager output,
  INetworkScanner localScanner,
  IInterfaceSubnetProvider interfaceSubnetProvider,
  ISpecFileProvider specProvider,
  ICluster cluster,
  IPeerMessageEnvelopeConverter converter
) : ICommandHandler<ScanParameters> {
  public async Task<int> Invoke( ScanParameters parameters, CancellationToken cancellationToken ) {
    output.Log.LogDebug( "Running scan command" );

    var inventory = await LoadInventoryAsync( parameters.SpecFile );
    if ( inventory == null ) {
      return ExitCodes.GeneralError;
    }

    var resolvedSubnets = await ResolveSubnetsAsync( inventory, cancellationToken );
    var scanRequest = BuildScanRequest( resolvedSubnets );

    PrintScanSummary( resolvedSubnets, scanRequest, inventory.Agents.Any() );

    var scanner = CreateScanner( inventory, resolvedSubnets );
    var uiTask = StartUi( parameters, inventory, scanner, scanRequest );

    Task.WaitAll( uiTask );

    output.Log.LogDebug( "scan command completed" );

    return ExitCodes.Success;
  }

  private async Task<Inventory?> LoadInventoryAsync( FileInfo? specFile ) {
    try {
      return await specProvider.GetDeserializedAsync( specFile );
    }
    catch ( FileNotFoundException ) {
      return null;
    }
  }

  private async Task<List<ResolvedSubnet>> ResolveSubnetsAsync( Inventory inventory, CancellationToken cancellationToken ) {
    var subnetProviders = BuildSubnetProviders( inventory, cancellationToken );
    var subnetProvider = new CompositeSubnetProvider( subnetProviders );

    output.Normal.WriteLineVerbose( $"Using {subnetProvider.GetType().Name}" );
    output.Log.LogDebug( "Using {SubnetProviderType}", subnetProvider.GetType().Name );

    return await subnetProvider.GetAsync();
  }

  private List<ISubnetProvider> BuildSubnetProviders( Inventory inventory, CancellationToken cancellationToken ) {
    var providers = new List<ISubnetProvider> { interfaceSubnetProvider };

    if ( inventory.Network != null ) {
      providers.Add( new PredefinedSubnetProvider( inventory.Network.Subnets ) );
    }

    if ( inventory.Agents.Any() ) {
      providers.Add( new AgentSubnetProvider(
        output.GetLogger(),
        inventory.Agents,
        cluster,
        cancellationToken
      ) );
    }

    return providers;
  }

  private static NetworkScanOptions BuildScanRequest( List<ResolvedSubnet> resolvedSubnets ) {
    var uniqueCidrs = resolvedSubnets
      .Select( rs => rs.Cidr )
      .Distinct()
      .ToList();

    return new NetworkScanOptions { Cidrs = uniqueCidrs };
  }

  private void PrintScanSummary( List<ResolvedSubnet> resolvedSubnets, NetworkScanOptions scanRequest, bool hasAgents ) {
    var groupedSubnets = resolvedSubnets
      .GroupBy( subnet => subnet.Cidr )
      .Select( group => new { Cidr = group.Key, Sources = group.Select( r => r.Source ).Distinct().ToList() } )
      .ToList();

    output.Normal.WriteLine(
      0,
      $"Scanning {groupedSubnets.Count} subnet{( groupedSubnets.Count > 1 ? "s" : string.Empty )}"
    );

    foreach ( var subnet in groupedSubnets ) {
      var sourceList = string.Join( ", ", subnet.Sources );
      output.Normal.Write( 1, $"{subnet.Cidr}", ConsoleColor.Cyan );
      output.Normal.WriteLine(
        " (" + IpNetworkUtils.GetIpRangeCount( subnet.Cidr ) +
        " addresses, estimated scan time is " +
        scanRequest.EstimatedDuration( subnet.Cidr ) +
        ")" +
        ( hasAgents ? $" via {sourceList}" : string.Empty ),
        ConsoleColor.DarkGray
      );
    }

    output.Log.LogInformation(
      "Scanning {SubnetCount} subnet(s): {SubnetList}",
      groupedSubnets.Count,
      string.Join( ", ", groupedSubnets.Select( s => s.Cidr ) )
    );
  }

  private INetworkScanner CreateScanner( Inventory inventory, List<ResolvedSubnet> resolvedSubnets ) {
    if ( !inventory.Agents.Any() ) {
      return localScanner;
    }

    return new DistributedNetworkScanner(
      localScanner,
      cluster,
      converter,
      resolvedSubnets,
      inventory,
      output.GetLogger()
    );
  }

  private Task<int> StartUi( ScanParameters parameters, Inventory inventory, INetworkScanner scanner, NetworkScanOptions scanRequest ) {
    if ( parameters.Interactive ) {
      var ui = new InteractiveUi(
        output,
        inventory.Network,
        scanner,
        scanRequest,
        new DefaultKeyMap(),
        parameters.ShowLogPanel
      );
      return ui.RunAsync();
    }
    else {
      var ui = new NonInteractiveUi( output, scanner );
      return ui.RunAsync( scanRequest, inventory.Network, parameters.OutputFormat );
    }
  }
}