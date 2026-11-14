using System.CommandLine;
using Drift.Cli.Abstractions;
using Drift.Cli.Commands.Common.Commands;
using Drift.Cli.Commands.Scan.Interactive;
using Drift.Cli.Commands.Scan.Interactive.Input;
using Drift.Cli.Commands.Scan.NonInteractive;
using Drift.Cli.Infrastructure;
using Drift.Cli.Presentation.Console.Logging;
using Drift.Cli.Presentation.Console.Managers.Abstractions;
using Drift.Cli.SpecFile;
using Drift.Common.Network;
using Drift.Coordinator.Client;
using Drift.Domain;
using Drift.Domain.Scan;
using Drift.Scanning.Subnets;
using Drift.Scanning.Subnets.Interface;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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

internal sealed class ScanCommandHandler(
  IOutputManager output,
  IScanOrchestrator localScanOrchestrator,
  IInterfaceSubnetProvider interfaceSubnetProvider,
  ISpecFileProvider specProvider,
  EnvironmentTargetProvider targetProvider,
  IServiceProvider serviceProvider
) : ICommandHandler<ScanParameters> {
  public async Task<int> Invoke( ScanParameters parameters, CancellationToken cancellationToken ) {
    output.Log.LogDebug( "Running scan command" );

    EnvironmentTarget target;
    try {
      target = targetProvider.Resolve();
    }
    catch ( Exception exception ) when ( exception is FormatException or InvalidOperationException ) {
      output.Normal.WriteLineError( exception.Message );
      return ExitCodes.GeneralError;
    }

    if ( target.Kind == EnvironmentTargetKind.Server ) {
      return await RunServerScanAsync( parameters, target, cancellationToken );
    }

    var (inventory, errorExitCode) = await LoadInventoryAsync( parameters.SpecFile );
    if ( errorExitCode != null ) {
      return errorExitCode.Value;
    }

    if ( inventory.Agents.Count > 0 ) {
      output.Normal.WriteLineWarning( "The local environment scans locally; declared agents are not used." );
    }

    var resolvedSubnets = await ResolveLocalSubnetsAsync( inventory );
    var scanRequest = BuildScanRequest( resolvedSubnets );
    PrintScanSummary( resolvedSubnets, scanRequest );
    var exitCode = await StartUiAsync( parameters, inventory, localScanOrchestrator, scanRequest, cancellationToken );

    output.Log.LogDebug( "scan command completed" );
    return exitCode;
  }

  private async Task<int> RunServerScanAsync(
    ScanParameters parameters,
    EnvironmentTarget target,
    CancellationToken cancellationToken
  ) {
    output.GetLogger().LogDebug( "Getting spec from active coordinator environment" );

    using var client = ControlApiClient.Create( target.ServerAddress! );
    var spec = await client.GetSpecAsync( cancellationToken );
    if ( spec is null ) {
      output.Normal.WriteLineError( "No spec has been uploaded to the coordinator." );
      return ExitCodes.GeneralError;
    }

    using var scanOrchestrator = new RemoteScanOrchestrator( client, output.GetLogger() );
    var inventory = new Inventory { Network = spec };
    var scanRequest = new NetworkScanOptions();

    var exitCode = await StartUiAsync( parameters, inventory, scanOrchestrator, scanRequest, cancellationToken );

    output.Log.LogDebug( "scan command completed" );
    return exitCode;
  }

  /// <returns>The loaded inventory, or an exit code if the spec could not be loaded (already reported).</returns>
  private async Task<(Inventory inventory, int? errorExitCode)> LoadInventoryAsync( FileInfo? specFile ) {
    var empty = new Inventory { Network = new Network(), Agents = [] };
    try {
      var loadedInventory = await specProvider.GetDeserializedAsync( specFile );
      return ( loadedInventory ?? empty, null );
    }
    catch ( FileNotFoundException ) {
      return ( empty, ExitCodes.GeneralError );
    }
    catch ( InvalidSpecException ) {
      return ( empty, ExitCodes.SpecValidationError );
    }
  }

  private async Task<List<ResolvedSubnet>> ResolveLocalSubnetsAsync( Inventory inventory ) {
    var localSubnets = await interfaceSubnetProvider.GetAsync();
    var declaredSubnets = inventory.Network.Subnets
      .Where( subnet => subnet.Enabled != false )
      .Select( subnet => new CidrBlock( subnet.Address ) )
      .ToHashSet();
    // Declared subnets that aren't on a local interface are still scanned: they may be reachable via routing
    // (gateway, VPN, static routes). MAC addresses aren't available for those, as ARP is link-local.
    var attachedSubnets = localSubnets.Select( subnet => subnet.Cidr ).ToHashSet();
    foreach ( var routedSubnet in declaredSubnets.Except( attachedSubnets ) ) {
      output.Normal.WriteLineWarning(
        $"Declared subnet {routedSubnet} is not directly attached; scanning via routing (MAC addresses unavailable)."
      );
      output.Log.LogWarning(
        "Declared subnet {Cidr} is not directly attached; scanning via routing (MAC addresses unavailable)",
        routedSubnet
      );
    }

    var subnetProvider = new CompositeSubnetProvider(
      [
        new PredefinedSubnetProvider( localSubnets.Select( subnet =>
          new DeclaredSubnet { Address = subnet.Cidr.ToString() } ) ),
        new PredefinedSubnetProvider( inventory.Network.Subnets )
      ]
    );

    output.Normal.WriteLineVerbose( $"Using {subnetProvider.GetType().Name}" );
    output.Log.LogDebug( "Using {SubnetProviderType}", subnetProvider.GetType().Name );
    return await subnetProvider.GetAsync();
  }

  private static NetworkScanOptions BuildScanRequest( List<ResolvedSubnet> resolvedSubnets ) {
    return new NetworkScanOptions { Cidrs = resolvedSubnets.Select( subnet => subnet.Cidr ).Distinct().ToList() };
  }

  private void PrintScanSummary( List<ResolvedSubnet> resolvedSubnets, NetworkScanOptions scanRequest ) {
    var groupedSubnets = resolvedSubnets
      .GroupBy( subnet => subnet.Cidr )
      .Select( group => group.Key )
      .ToList();

    output.Normal.WriteLine(
      0,
      $"Scanning {groupedSubnets.Count} subnet{( groupedSubnets.Count > 1 ? "s" : string.Empty )}"
    );

    foreach ( var cidr in groupedSubnets ) {
      output.Normal.Write( 1, $"{cidr}", ConsoleColor.Cyan );
      output.Normal.WriteLine(
        " (" + IpNetworkUtils.GetIpRangeCount( cidr ) +
        " addresses, estimated scan time is " +
        scanRequest.EstimatedDuration( cidr ) + ")",
        ConsoleColor.DarkGray
      );
    }

    output.Log.LogInformation(
      "Scanning {SubnetCount} subnet(s): {SubnetList}",
      groupedSubnets.Count,
      string.Join( ", ", groupedSubnets )
    );
  }

  private async Task<int> StartUiAsync(
    ScanParameters parameters,
    Inventory inventory,
    IScanOrchestrator scanOrchestrator,
    NetworkScanOptions scanRequest,
    CancellationToken cancellationToken
  ) {
    if ( parameters.Interactive ) {
      await using var ui = new InteractiveUi(
        output,
        inventory.Network,
        scanOrchestrator,
        scanRequest,
        new DefaultKeyMap(),
        parameters.ShowLogPanel,
        serviceProvider.GetRequiredService<IConsoleKeyWatcher>(),
        serviceProvider.GetRequiredService<IConsoleResizeWatcher>()
      );
      return await ui.RunAsync( cancellationToken );
    }
    else {
      var ui = new NonInteractiveUi( output, scanOrchestrator );
      return await ui.RunAsync( scanRequest, inventory.Network, parameters.OutputFormat, cancellationToken );
    }
  }
}