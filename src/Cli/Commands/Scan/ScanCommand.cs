using System.CommandLine;
using System.Globalization;
using Drift.Cli.Abstractions;
using Drift.Cli.Commands.Common;
using Drift.Cli.Commands.Init;
using Drift.Cli.Commands.Scan.Rendering;
using Drift.Cli.Commands.Scan.Subnet;
using Drift.Cli.Output;
using Drift.Cli.Output.Abstractions;
using Drift.Cli.Renderer;
using Drift.Cli.Scan;
using Drift.Domain;
using Drift.Domain.Progress;
using Drift.Domain.Scan;
using Drift.Utils;
using Humanizer;
using Humanizer.Localisation;
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
internal class ScanCommand( IServiceProvider provider ) : CommandBase<ScanParameters, ScanCommandHandler>(
  "scan",
  "Scan the network and detect drift",
  provider
) {
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

public class ScanCommandHandler(
  IOutputManager output,
  INetworkScanner scanner,
  IInterfaceSubnetProvider interfaceSubnetProvider
) : ICommandHandler<ScanParameters> {
  public async Task<int> Invoke( ScanParameters parameters, CancellationToken cancellationToken ) {
    output.Log.LogDebug( "Running scan command" );

    Network? network;

    try {
      network = SpecFileDeserializer.Deserialize( parameters.SpecFile, output )?.Network;
    }
    catch ( FileNotFoundException ) {
      return ExitCodes.GeneralError;
    }

    //TODO use both declared and discovered subnets
    ISubnetProvider subnetProvider = network == null
      ? interfaceSubnetProvider
      : new DeclaredSubnetProvider( network.Subnets.Where( s => s.Enabled ?? true ) );

    output.Normal.WriteLineVerbose( $"Using subnet provider: {subnetProvider.GetType().Name}" );
    output.Log.LogDebug( "Using subnet provider: {SubnetProviderType}", subnetProvider.GetType().Name );

    var subnets = subnetProvider.Get();

    output.Normal.WriteLine( 0,
      $"Scanning {subnets.Count} subnet{( subnets.Count > 1 ? "s" : "" )}" ); // TODO many more varieties
    foreach ( var cidr in subnets ) {
      //TODO write name if from spec: Ui.WriteLine( 1, $"{subnet.Id}: {subnet.Network}" );
      output.Normal.Write( 1, $"{cidr}", ConsoleColor.Cyan );
      output.Normal.WriteLine(
        " (" + IpNetworkUtils.GetIpRangeCount( cidr ) +
        " addresses, estimated scan time is " +
        InitCommandHandler.CalculateScanDuration(
          cidr,
          PingNetworkScanner.MaxPingsPerSecond
        ) + // TODO .Humanize( 2, CultureInfo.InvariantCulture, minUnit: TimeUnit.Second )
        ")", ConsoleColor.DarkGray );
    }

    output.Log.LogInformation(
      "Scanning {SubnetCount} subnet(s): {SubnetList}", subnets.Count,
      string.Join( ", ", subnets )
    );

    ScanResult? scanResult = null;

    if ( output.Is( OutputFormat.Normal ) ) {
      var dCol = new TaskDescriptionColumn();
      dCol.Alignment = Justify.Right;
      var pCol = new PercentageColumn();
      pCol.Style = new Style( Color.Cyan1 );
      pCol.CompletedStyle = new Style( Color.Green1 );
      scanResult = await AnsiConsole.Progress()
        .AutoClear( true )
        .Columns( dCol, pCol )
        .StartAsync( async ctx => {
          var progressBars = new Dictionary<string, ProgressTask>();
          progressBars["Ping Scan"] = ctx.AddTask( "Ping Scan" );
          //progressBars["DNS resolution"] = ctx.AddTask( "DNS resolution" );
          //progressBars["Connect Scan"] = ctx.AddTask( "Connect Scan" );

          return await scanner.ScanAsync( subnets, onProgress: progressReport => {
            UpdateProgressBar( progressReport, ctx, progressBars );
          }, cancellationToken: CancellationToken.None );
        } );
    }

    if ( output.Is( OutputFormat.Log ) ) {
      var lastLogTime = DateTime.MinValue;
      var completedTasks = new HashSet<string>();

      scanResult = await scanner.ScanAsync( subnets, onProgress: progressReport => {
        UpdateProgressLog( progressReport, output, ref lastLogTime, ref completedTasks );
      }, cancellationToken: CancellationToken.None );
    }

    if ( scanResult == null ) {
      throw new Exception( "Scan result is null" );
    }

    output.Log.LogInformation( "Scan completed" );

    IRenderer<ScanRenderData> renderer =
      parameters.OutputFormat switch {
        OutputFormat.Normal => new NormalRenderer( output.Normal ),
        OutputFormat.Log => new LogRenderer( output.Log ),
        _ => new NullRenderer<ScanRenderData>()
      };

    output.Log.LogDebug( "Render result using {RendererType}", renderer.GetType().Name );

    output.Normal.WriteLine();

    renderer.Render(
      new ScanRenderData {
        DevicesDiscovered = scanResult.DiscoveredDevices,
        DevicesDeclared = network == null ? [] : network.Devices.Where( d => d.Enabled ?? true )
      } /*, output.Log*/
    );

    output.Log.LogDebug( "Scan command completed" );

    return ExitCodes.Success;

    void UpdateProgressBar(
      ProgressReport progressReport,
      ProgressContext context,
      Dictionary<string, ProgressTask> progressBars
    ) {
      foreach ( var taskProgress in progressReport.Tasks ) {
        //TODO hack
        var transformedTaskName = taskProgress.TaskName.Contains( "DNS" ) ? "DNS resolution" : taskProgress.TaskName;

        if ( !progressBars.TryGetValue( transformedTaskName, out var bar ) ) {
          bar = context.AddTask( $"{transformedTaskName}" );
          progressBars[transformedTaskName] = bar;
        }

        if ( !bar.IsFinished ) {
          // Spectre's max value is 100 by default
          bar.Value = Math.Min( taskProgress.CompletionPct, 100 );
        }
      }
    }
  }


  //TODO make private
  internal static void UpdateProgressLog(
    ProgressReport progressReport,
    IOutputManager output,
    ref DateTime lastLogTime,
    ref HashSet<string> completedTasks
  ) {
    var now = DateTime.UtcNow;
    bool shouldLog = ( now - lastLogTime ).TotalSeconds >= 1;

    if ( shouldLog ) {
      foreach ( var taskProgress in progressReport.Tasks ) {
        //TODO hack
        var transformedTaskName = taskProgress.TaskName.Contains( "DNS" ) ? "DNS resolution" : taskProgress.TaskName;
        if ( completedTasks.Contains( transformedTaskName ) ) {
          continue;
        }

        output.Log.LogInformation( "{TaskName}: {CompletionPct}%", transformedTaskName, taskProgress.CompletionPct );

        if ( taskProgress.CompletionPct >= 100 ) {
          completedTasks.Add( transformedTaskName );
        }

        Thread.Sleep( 500 ); //TODO remove
      }
    }

    if ( shouldLog ) {
      lastLogTime = now;
    }
  }
}