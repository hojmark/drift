namespace Drift.Cli.Commands.Scan.Interactive;

using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

public class AsyncKeyInputWatcher : IAsyncDisposable {
  private readonly ConcurrentQueue<ConsoleKey> _keyBuffer = new();
  private readonly CancellationTokenSource _cts = new();
  private readonly Task _listenerTask;

  private TaskCompletionSource? _waiter;

  public AsyncKeyInputWatcher() {
    _listenerTask = Task.Run( ListenLoopAsync );
  }

  private async Task ListenLoopAsync() {
    while ( !_cts.Token.IsCancellationRequested ) {
      if ( Console.KeyAvailable ) {
        var key = Console.ReadKey( intercept: true ).Key;
        _keyBuffer.Enqueue( key );

        _waiter?.TrySetResult();
      }

      await Task.Delay( 10, _cts.Token ).ConfigureAwait( false );
    }
  }

  /// <summary>
  /// Awaits the next keypress (or returns immediately if a key is already buffered).
  /// </summary>
  public Task WaitForNextKeyAsync() {
    if ( !_keyBuffer.IsEmpty )
      return Task.CompletedTask;

    _waiter = new(TaskCreationOptions.RunContinuationsAsynchronously);
    return _waiter.Task;
  }

  /// <summary>
  /// Returns the next keypress from the buffer, or null if none.
  /// </summary>
  public ConsoleKey? ConsumeKey() {
    return _keyBuffer.TryDequeue( out var key ) ? key : null;
  }

  public async ValueTask DisposeAsync() {
    _cts.Cancel();

    try {
      await _listenerTask;
    }
    catch ( TaskCanceledException ) {
    }

    _cts.Dispose();
  }
}