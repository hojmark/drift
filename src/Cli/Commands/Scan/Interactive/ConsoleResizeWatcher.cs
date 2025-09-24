namespace Drift.Cli.Commands.Scan.Interactive;

using System;
using System.Threading;
using System.Threading.Tasks;

internal sealed class ConsoleResizeWatcher : IDisposable {
  private int _lastWidth;
  private int _lastHeight;
  private readonly TimeSpan _pollInterval = TimeSpan.FromMilliseconds( 200 );
  private TaskCompletionSource? _resizeSignal;
  private readonly CancellationTokenSource _cts = new();

  public ConsoleResizeWatcher() {
    _lastWidth = Console.WindowWidth;
    _lastHeight = Console.WindowHeight;
    _ = WatchLoopAsync();
  }

  private async Task WatchLoopAsync() {
    //TODO catch exceptions and expose
    while ( !_cts.IsCancellationRequested ) {
      await Task.Delay( _pollInterval, _cts.Token );

      if ( _cts.IsCancellationRequested ) {
        continue;
      }

      int currentWidth = Console.WindowWidth;
      int currentHeight = Console.WindowHeight;

      if ( currentWidth != _lastWidth || currentHeight != _lastHeight ) {
        _lastWidth = currentWidth;
        _lastHeight = currentHeight;

        _resizeSignal?.TrySetResult();
      }
    }
  }

  // TODO Note: there's a risk that the caller will miss the resize event if it's not constantly waiting (like in the case of render loop)
  public Task WaitForResizeAsync() {
    if ( _resizeSignal == null || _resizeSignal.Task.IsCompleted ) {
      _resizeSignal = new TaskCompletionSource();
    }

    return _resizeSignal.Task;
  }

  public void Dispose() => _cts.Cancel();
}