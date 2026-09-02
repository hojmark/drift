using System.Collections.Concurrent;

namespace Drift.Cli.Commands.Scan.Interactive.Input;

/// <inheritdoc/>
internal sealed class ConsoleKeyWatcher : IConsoleKeyWatcher {
  private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds( 50 );

  private readonly ConcurrentQueue<ConsoleKey> _keyBuffer = new();
  private readonly CancellationTokenSource _cts = new();
  private readonly Task _listenerTask;
  private TaskCompletionSource? _waiter;

  public ConsoleKeyWatcher() {
    _listenerTask = Task.Run( ListenLoopAsync, _cts.Token );
  }

  public Task WaitForKeyAsync() {
    if ( !_keyBuffer.IsEmpty ) {
      return Task.CompletedTask;
    }

    _waiter = new(TaskCreationOptions.RunContinuationsAsynchronously);
    return _waiter.Task;
  }

  public ConsoleKey? Consume() {
    return _keyBuffer.TryDequeue( out var key ) ? key : null;
  }

  public async ValueTask DisposeAsync() {
    await _cts.CancelAsync();

    try {
      await _listenerTask;
    }
    catch ( TaskCanceledException ) {
      // Expected when task is cancelled
    }

    _cts.Dispose();
  }

  private async Task ListenLoopAsync() {
    while ( !_cts.Token.IsCancellationRequested ) {
      if ( Console.KeyAvailable ) {
        var key = Console.ReadKey( intercept: true ).Key;
        _keyBuffer.Enqueue( key );
        _waiter?.TrySetResult();
      }
      else {
        try {
          await Task.Delay( PollInterval, _cts.Token );
        }
        catch ( OperationCanceledException ) when ( _cts.IsCancellationRequested ) {
          // Expected when the watcher is disposed.
        }
      }
    }
  }
}