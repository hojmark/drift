using Drift.Cli.Abstractions;
using Drift.Cli.Commands.Scan.Models;
using Drift.Cli.Commands.Scan.Rendering;
using Drift.Cli.Commands.Scan.ResultProcessors;
using Drift.Cli.Presentation.Console;
using Drift.Cli.Presentation.Console.Logging;
using Drift.Cli.Presentation.Console.Managers.Abstractions;
using Drift.Cli.Presentation.Rendering;
using Drift.Domain;
using Drift.Domain.Scan;
using Spectre.Console;

namespace Drift.Cli.Commands.Scan.NonInteractive;

internal sealed class NonInteractiveUi( IOutputManager output, INetworkScanner scanner ) {
  // TODO make private
  internal static void UpdateProgressDebounced(
    Percentage progress,
    Action<Percentage> output,
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

    output( progress );
    lastLogTime = now;
  }

  internal async Task<int> RunAsync(
    NetworkScanOptions scanRequest,
    Network? network,
    OutputFormat outputFormat
  ) {
    var result = await PerformScanAsync( scanRequest );

    output.Log.LogInformation( "Scan completed" );

    var uiSubnets = NetworkScanResultProcessor.Process( result, network );

    IRenderer<List<Subnet>> renderer =
      outputFormat switch {
        OutputFormat.Normal => new NormalScanRenderer( output.Normal ),
        OutputFormat.Log => new LogScanRenderer( output.Log ),
        _ => new NullRenderer<IList<Subnet>>()
      };

    output.Log.LogDebug( "Render result using {RendererType}", renderer.GetType().Name );

    output.Normal.WriteLine();

    renderer.Render( uiSubnets );

    return ExitCodes.Success;
  }

  private async Task<NetworkScanResult> PerformScanAsync( NetworkScanOptions request ) {
    if ( output.Is( OutputFormat.Normal ) ) {
      var dCol = new TaskDescriptionColumn { Alignment = Justify.Right };
      var pCol = new PercentageColumn { Style = new Style( Color.Cyan1 ), CompletedStyle = new Style( Color.Green1 ) };

      return await AnsiConsole.Progress()
        .AutoClear( true )
        .Columns( dCol, pCol )
        .StartAsync( async ctx => {
          var progressBar = ctx.AddTask( "Ping Scan" );

          EventHandler<NetworkScanResult> updater = ( _, r ) => {
            progressBar.Value = r.Progress;
          };

          try {
            scanner.ResultUpdated += updater;
            return await scanner.ScanAsync( request, output.GetLogger() );
          }
          finally {
            scanner.ResultUpdated -= updater;
          }
        } );
    }

    if ( output.Is( OutputFormat.Log ) ) {
      var lastLogTime = DateTime.MinValue;

      // TODO refactor to PerformScan like in InitCommand

      EventHandler<NetworkScanResult> updater = ( _, r ) => {
        UpdateProgressDebounced(
          r.Progress,
          progress => output.Log.LogInformation( "{TaskName}: {CompletionPct}", "Ping Scan", progress ),
          ref lastLogTime
        );
      };

      try {
        scanner.ResultUpdated += updater;
        return await scanner.ScanAsync( request, output.GetLogger() );
      }
      finally {
        scanner.ResultUpdated -= updater;
      }
    }

    throw new NotImplementedException();
  }
}