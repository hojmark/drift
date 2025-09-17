using System.CommandLine;
using Drift.Cli.Abstractions;
using Drift.Cli.Commands.Common;
using Drift.Cli.Commands.Init;
using Drift.Cli.Commands.Scan.New;
using Drift.Cli.Commands.Scan.Rendering;
using Drift.Cli.Output;
using Drift.Cli.Output.Abstractions;
using Drift.Cli.Output.Loggers;
using Drift.Cli.Renderer;
using Drift.Core.Scan;
using Drift.Core.Scan.Model;
using Drift.Core.Scan.Subnet;
using Drift.Domain;
using Drift.Domain.NeoProgress;
using Drift.Domain.Progress;
using Drift.Utils;
using Drift.Utils.Tools;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using ProgressReport = Drift.Domain.Progress.ProgressReport;

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
  public ScanCommand( IServiceProvider provider ) : base( "scan",
    "Scan the network and detect drift",
    provider ) {
    Add( ScanParameters.Options.Interactive);
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

enum DeviceStatus {
  Online,
  Offline,
  Unknown
}

class Device {
  public string IP {
    get;
    set;
  } = "";

  public string Name {
    get;
    set;
  } = "";

  public DeviceStatus Status {
    get;
    set;
  }
}

public class ScanCommandHandler(
  IOutputManager output,
  INetworkScanner scanner,
  IScanService scanService,
  IInterfaceSubnetProvider interfaceSubnetProvider,
  ISpecFileProvider specProvider
) : ICommandHandler<ScanParameters> {
  private ProgressNode _liveData;

  public async Task<int> Invoke( ScanParameters parameters, CancellationToken cancellationToken ) {
    output.Log.LogDebug( "Running scan command" );

    Network? network;

    try {
      network = ( await specProvider.GetDeserializedAsync( parameters.SpecFile ) )?.Network;
    }
    catch ( FileNotFoundException ) {
      return ExitCodes.GeneralError;
    }

    if ( parameters.Interactive.HasValue && parameters.Interactive.Value ) {
      NewScanUi.Show();

      return ExitCodes.Success;
    }

    var scanTask = scanService.ScanAsync(
      new ScanRequest { Spec = network },
      onProgress => {
        _liveData = onProgress;
      },
      output.GetCompoundLogger(),
      cancellationToken
    );

    var subnets = new[] { "192.168.1.0/24", "10.0.0.0/16", "172.16.0.0/24" };
    //var subnetsn = _liveData.GetChild( ScanPaths.SubnetDiscovery.FromInterfaces )!
    //.GetContext( ScanPaths.SubnetDiscovery.ContextKeys.InterfaceSubnets ).Select( s => s.ToString() ).ToArray();

    var subnetDevices = new Dictionary<string, List<Device>>();
    var subnetExpanded = new Dictionary<string, bool>();
    var keyToSubnet = new Dictionary<ConsoleKey, string> {
      [ConsoleKey.D1] = subnets[0] /*, [ConsoleKey.D2] = subnets[1], [ConsoleKey.D3] = subnets[2],*/
    };

    foreach ( var subnet in subnets ) {
      subnetDevices[subnet] = new List<Device>();
      subnetExpanded[subnet] = true;
    }

    // Flags
    bool stopRequested = false;
    bool uiNeedsUpdate = true;

    // Start key listener
    Task.Run( () => {
      while ( !stopRequested ) {
        var key = Console.ReadKey( true ).Key;

        if ( key == ConsoleKey.Q ) {
          stopRequested = true;
        }
        else if ( keyToSubnet.TryGetValue( key, out var subnet ) ) {
          subnetExpanded[subnet] = !subnetExpanded[subnet];
          uiNeedsUpdate = true;
        }
      }
    } );

    // Start live UI
    AnsiConsole.Live( new Panel( "Starting..." ) )
      .AutoClear( false )
      .Start( ctx => {
        int counter = 1;
        var rng = new Random();
        DateTime lastDeviceTime = DateTime.UtcNow;

        while ( !stopRequested ) {
          // Simulate device discovery every 1.5s
          if ( ( DateTime.UtcNow - lastDeviceTime ).TotalSeconds >= 1.5 ) {
            var subnet = subnets[counter % subnets.Length];
            var ip = subnet.Replace( ".0/24", $".{counter}" );
            var status = (DeviceStatus) ( rng.Next( 0, 3 ) );

            subnetDevices[subnet].Add( new Device { IP = ip, Name = $"device-{counter}", Status = status } );

            counter++;
            lastDeviceTime = DateTime.UtcNow;
            uiNeedsUpdate = true;
          }

          if ( uiNeedsUpdate ) {
            // Build tree
            var tree = new Tree( "[bold]Scanning Subnets[/]" ).Style( "green" );

            int i = 1;
            int totalOnline = 0, totalOffline = 0, totalUnknown = 0;

            foreach ( var subnet in subnets ) {
              string title = $"{i++}. [blue]{subnet}[/] ({subnetDevices[subnet].Count} devices)";
              var node = tree.AddNode( title );
              node.Expanded = subnetExpanded[subnet];

              foreach ( var dev in subnetDevices[subnet] ) {
                string icon = dev.Status switch {
                  DeviceStatus.Online => ":green_circle:",
                  DeviceStatus.Offline => ":red_circle:",
                  _ => ":question:"
                };

                if ( dev.Status == DeviceStatus.Online ) totalOnline++;
                else if ( dev.Status == DeviceStatus.Offline ) totalOffline++;
                else totalUnknown++;

                node.AddNode( $"{icon} [yellow]{dev.IP}[/] ({dev.Name})" );
              }
            }

            // Build BreakdownChart
            var chart = new BreakdownChart()
              .Width( 60 )
              .AddItem( "🟢 Online", totalOnline, Color.Green )
              .AddItem( "🔴 Offline", totalOffline, Color.Red )
              .AddItem( "❓ Unknown", totalUnknown, Color.Grey );

            // Compose layout
            var layout = new Grid();
            layout.AddColumn();
            layout.AddRow( tree );
            layout.AddEmptyRow();
            layout.AddRow( chart );
            layout.AddRow( new Markup( "[dim]Press 1-3 to toggle subnets, Q to quit[/]" ) );

            ctx.UpdateTarget( layout );
            uiNeedsUpdate = false;
          }

          Thread.Sleep( 50 ); // keep it responsive
        }
      } );

    AnsiConsole.MarkupLine( "[bold green]Scan stopped by user.[/]" );

    return ExitCodes.Success;
/*
    var subnetProviders = new List<ISubnetProvider> { interfaceSubnetProvider };
    if ( network != null ) {
      subnetProviders.Add( new DeclaredSubnetProvider( network.Subnets ) );
    }

    var subnetProvider = new CompositeSubnetProvider( subnetProviders );

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
*/

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
          //progressBars["Ping Scan"] = ctx.AddTask( "Ping Scan" );
          //progressBars["DNS resolution"] = ctx.AddTask( "DNS resolution" );
          //progressBars["Connect Scan"] = ctx.AddTask( "Connect Scan" );

          return ( await scanService.ScanAsync( new ScanRequest { Spec = network }, onProgress => {
              UpdateProgressBar2( onProgress, ctx, progressBars );
            },
            output.GetCompoundLogger()
          ) ).Result;

          /*return await scanner.ScanAsync( subnets, output.GetCompoundLogger(), onProgress: progressReport => {
            UpdateProgressBar( progressReport, ctx, progressBars );
          }, cancellationToken: CancellationToken.None );*/
        } );
    }

    if ( output.Is( OutputFormat.Log ) ) {
      var lastLogTime = DateTime.MinValue;
      var completedTasks = new HashSet<string>();

      /*scanResult = await scanner.ScanAsync( subnets, output.GetCompoundLogger(), onProgress: progressReport => {
        UpdateProgressLog( progressReport, output, ref lastLogTime, ref completedTasks );
      }, cancellationToken: CancellationToken.None );*/
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

    void UpdateProgressBar2(
      ProgressNode progressReport,
      ProgressContext context,
      Dictionary<string, ProgressTask> progressBars
    ) {
      foreach ( var taskProgress in progressReport.GetChild( ScanPaths.DeviceDiscovery.Self ).Descendants ) {
        //TODO hack
        //var transformedTaskName = taskProgress.TaskName.Contains( "DNS" ) ? "DNS resolution" : taskProgress.TaskName;
        var transformedTaskName = taskProgress.Path.GetLastSegment();
        if ( !progressBars.TryGetValue( transformedTaskName, out var bar ) ) {
          bar = context.AddTask( $"{transformedTaskName}" );
          progressBars[transformedTaskName] = bar;
        }

        if ( !bar.IsFinished ) {
          // Spectre's max value is 100 by default
          bar.Value = Math.Min( taskProgress.TotalProgress, 100 );
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