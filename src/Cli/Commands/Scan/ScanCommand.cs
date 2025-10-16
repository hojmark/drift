using System.CommandLine;
using Drift.Cli.Abstractions;
using Drift.Cli.Commands.Common;
using Drift.Cli.Commands.Scan.Interactive;
using Drift.Cli.Commands.Scan.Interactive.Input;
using Drift.Cli.Commands.Scan.NonInteractive;
using Drift.Cli.Presentation.Console.Managers.Abstractions;
using Drift.Cli.SpecFile;
using Drift.Common.Network;
using Drift.Domain;
using Drift.Domain.Scan;
using Drift.Scanning.Subnets;
using Drift.Scanning.Subnets.Interface;
using Microsoft.Extensions.Logging;

namespace Drift.Cli.Commands.Scan;

/*
 * Ideas:
 *   Interactive mode:
 *   ➤ New host found: 192.168.1.42
 *   ➤ Port 22 no longer open on 192.168.1.10
 *   → Would you like to update the declared state? [y/N]

 *   Monitor mode:
 *     drift monitor --reference declared.yaml --interval 10m --notify slack,email,log,webhook
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
  INetworkScanner scanner,
  IInterfaceSubnetProvider interfaceSubnetProvider,
  ISpecFileProvider specProvider
) : ICommandHandler<ScanParameters> {
  public async Task<int> Invoke( ScanParameters parameters, CancellationToken cancellationToken ) {
    output.Log.LogDebug( "Running scan command" );

    Network? network;

    try {
      network = ( await specProvider.GetDeserializedAsync( parameters.SpecFile ) )?.Network;
    }
    catch ( FileNotFoundException ) {
      return ExitCodes.GeneralError;
    }

    var subnetProviders = new List<ISubnetProvider> { interfaceSubnetProvider };
    if ( network != null ) {
      subnetProviders.Add( new PredefinedSubnetProvider( network.Subnets ) );
    }

    var subnetProvider = new CompositeSubnetProvider( subnetProviders );

    output.Normal.WriteLineVerbose( $"Using {subnetProvider.GetType().Name}" );
    output.Log.LogDebug( "Using {SubnetProviderType}", subnetProvider.GetType().Name );

    var subnets = subnetProvider.Get();

    var scanRequest = new NetworkScanOptions { Cidrs = subnets };

    // TODO many more varieties
    output.Normal.WriteLine( 0, $"Scanning {subnets.Count} subnet{( subnets.Count > 1 ? "s" : string.Empty )}" );
    foreach ( var cidr in subnets ) {
      // TODO write name if from spec: Ui.WriteLine( 1, $"{subnet.Id}: {subnet.Network}" );
      output.Normal.Write( 1, $"{cidr}", ConsoleColor.Cyan );
      output.Normal.WriteLine(
        " (" + IpNetworkUtils.GetIpRangeCount( cidr ) +
        " addresses, estimated scan time is " +
        scanRequest.EstimatedDuration(
          cidr ) + // TODO .Humanize( 2, CultureInfo.InvariantCulture, minUnit: TimeUnit.Second )
        ")", ConsoleColor.DarkGray );
    }

    output.Log.LogInformation(
      "Scanning {SubnetCount} subnet(s): {SubnetList}",
      subnets.Count,
      string.Join( ", ", subnets )
    );

    Task<int> uiTask;

    if ( parameters.Interactive ) {
      var ui = new InteractiveUi( output, network, scanner, scanRequest, new DefaultKeyMap(), parameters.ShowLogPanel );
      uiTask = ui.RunAsync();
    }
    else {
      var ui = new NonInteractiveUi( output, scanner );
      uiTask = ui.RunAsync( scanRequest, network, parameters.OutputFormat );
    }

    Task.WaitAll( uiTask );

    output.Log.LogDebug( "scan command completed" );

    return ExitCodes.Success;
  }
}