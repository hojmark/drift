namespace Drift.Cli.Commands.Scan.Interactive.Input;

internal sealed class ConsoleResizeWatcher : IDisposable {
  private readonly TimeSpan _pollInterval = TimeSpan.FromMilliseconds( 200 );
  private readonly CancellationTokenSource _cts = new();
  private int _lastWidth;
  private int _lastHeight;
  private TaskCompletionSource? _resizeSignal;

  public ConsoleResizeWatcher() {
    _lastWidth = Console.WindowWidth;
    _lastHeight = Console.WindowHeight;
    _ = WatchLoopAsync();
  }

  // TODO Note: there's a risk that the caller will miss the resize event if it's not constantly waiting (like in the case of render loop)
  public Task WaitForResizeAsync() {
    if ( _resizeSignal == null || _resizeSignal.Task.IsCompleted ) {
      _resizeSignal = new TaskCompletionSource();
    }

    return _resizeSignal.Task;
  }

  public void Dispose() => _cts.Dispose();

  private async Task WatchLoopAsync() {
    // TODO catch exceptions and expose
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
}