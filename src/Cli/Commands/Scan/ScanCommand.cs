using System.CommandLine;
using Drift.Cli.Abstractions;
using Drift.Cli.Commands.Common;
using Drift.Cli.Commands.Scan.Interactive;
using Drift.Cli.Commands.Scan.Interactive.KeyMaps;
using Drift.Cli.Commands.Scan.Rendering;
using Drift.Cli.Output;
using Drift.Cli.Output.Abstractions;
using Drift.Cli.Output.Logging;
using Drift.Cli.Renderer;
using Drift.Core.Scan.Subnets;
using Drift.Core.Scan.Subnets.Interface;
using Drift.Domain;
using Drift.Domain.Scan;
using Drift.Utils;
using Microsoft.Extensions.Logging;
using Spectre.Console;

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

  private enum ShowMode {
    All = 1,
    Changed = 2,
    Unchanged = 3
  }

  //var monitorOption = new Option<bool>( "--monitor", "Continually scan network(s) until manually stopped." );
  //AddOption( monitorOption );
  //var monitorIntervalOption = new Option<TimeSpan>( "--interval", "Scan interval when in monitor mode." );
  //AddOption( monitorIntervalOption );
  //var monitorNotifyOption = new Option<string>( "--notify", "Notification channels when in monitor mode." );
  // "save" or "update" instead?
  //AddOption( new Option<bool>( "--write", "Create or update reference with discovered resources (devices, subnets etc.)." ) );
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
      subnetProviders.Add( new DeclaredSubnetProvider( network.Subnets ) );
    }

    var subnetProvider = new CompositeSubnetProvider( subnetProviders );

    output.Normal.WriteLineVerbose( $"Using subnet provider: {subnetProvider.GetType().Name}" );
    output.Log.LogDebug( "Using subnet provider: {SubnetProviderType}", subnetProvider.GetType().Name );

    var subnets = subnetProvider.Get();

    var scanRequest = new NetworkScanOptions { Cidrs = subnets };

    if ( parameters.Interactive ) {
      var ui = new InteractiveUi( output, scanner, scanRequest, new DefaultKeyMap() );
      return await ui.RunAsync();
    }

    output.Normal.WriteLine( 0,
      $"Scanning {subnets.Count} subnet{( subnets.Count > 1 ? "s" : "" )}" ); // TODO many more varieties
    foreach ( var cidr in subnets ) {
      //TODO write name if from spec: Ui.WriteLine( 1, $"{subnet.Id}: {subnet.Network}" );
      output.Normal.Write( 1, $"{cidr}", ConsoleColor.Cyan );
      output.Normal.WriteLine(
        " (" + IpNetworkUtils.GetIpRangeCount( cidr ) +
        " addresses, estimated scan time is " +
        scanRequest.EstimatedDuration(
          cidr ) + // TODO .Humanize( 2, CultureInfo.InvariantCulture, minUnit: TimeUnit.Second )
        ")", ConsoleColor.DarkGray );
    }

    output.Log.LogInformation(
      "Scanning {SubnetCount} subnet(s): {SubnetList}", subnets.Count,
      string.Join( ", ", subnets )
    );

    NetworkScanResult? scanResult = null;

    if ( output.Is( OutputFormat.Normal ) ) {
      var dCol = new TaskDescriptionColumn { Alignment = Justify.Right };
      var pCol = new PercentageColumn { Style = new Style( Color.Cyan1 ), CompletedStyle = new Style( Color.Green1 ) };
      scanResult = await AnsiConsole.Progress()
        .AutoClear( true )
        .Columns( dCol, pCol )
        .StartAsync( async ctx => {
          var progressBar = ctx.AddTask( "Ping Scan" );

          EventHandler<NetworkScanResult> updater = ( _, r ) => {
            progressBar.Value = r.Progress;
          };

          scanner.ResultUpdated += updater;

          try {
            return await scanner.ScanAsync( scanRequest, output.GetLogger(),
              cancellationToken: CancellationToken.None );
          }
          finally {
            scanner.ResultUpdated -= updater;
          }
        } );
    }

    if ( output.Is( OutputFormat.Log ) ) {
      var lastLogTime = DateTime.MinValue;

      EventHandler<NetworkScanResult> updater = ( _, r ) => {
        UpdateProgressLog( r.Progress, output, ref lastLogTime );
      };

      scanner.ResultUpdated += updater;

      try {
        scanResult =
          await scanner.ScanAsync( scanRequest, output.GetLogger(), cancellationToken: CancellationToken.None );
      }
      finally {
        scanner.ResultUpdated -= updater;
      }
    }

    if ( scanResult == null ) {
      throw new Exception( "Scan result is null" );
    }

    output.Log.LogInformation( "Scan completed" );

    IRenderer<ScanRenderData> renderer =
      parameters.OutputFormat switch {
        OutputFormat.Normal => new NormalScanRenderer( output.Normal ),
        OutputFormat.Log => new LogScanRenderer( output.Log ),
        _ => new NullRenderer<ScanRenderData>()
      };

    output.Log.LogDebug( "Render result using {RendererType}", renderer.GetType().Name );

    output.Normal.WriteLine();

    renderer.Render(
      new ScanRenderData {
        DevicesDiscovered = scanResult.Subnets.SelectMany( s => s.DiscoveredDevices ),
        DevicesDeclared = network == null ? [] : network.Devices.Where( d => d.Enabled ?? true )
      }
    );

    output.Log.LogDebug( "Scan command completed" );

    return ExitCodes.Success;
  }

  //TODO make private
  internal static void UpdateProgressLog(
    Percentage progress,
    IOutputManager output,
    ref DateTime lastLogTime
  ) {
    var now = DateTime.UtcNow;
    bool shouldLog = ( now - lastLogTime ).TotalSeconds >= 1 ||
                     // Always log start/end
                     progress == Percentage.Zero ||
                     progress == Percentage.Hundred;

    if ( !shouldLog ) {
      return;
    }

    output.Log.LogInformation( "{TaskName}: {CompletionPct}", "Ping Scan", progress );
    lastLogTime = now;
  }
}