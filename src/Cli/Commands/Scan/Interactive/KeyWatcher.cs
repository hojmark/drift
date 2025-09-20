namespace Drift.Cli.Commands.Scan.Interactive;

using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

internal sealed class AsyncKeyInputWatcher : IAsyncDisposable {
  private readonly ConcurrentQueue<ConsoleKey> _keyBuffer = new();
  private readonly CancellationTokenSource _cts = new();
  private readonly Task _listenerTask;

  private TaskCompletionSource? _waiter;

  public AsyncKeyInputWatcher() {
    _listenerTask = Task.Run( ListenLoopAsync );
  }

  private void ListenLoopAsync() {
    while ( !_cts.Token.IsCancellationRequested ) {
      var key = Console.ReadKey( intercept: true ).Key;
      _keyBuffer.Enqueue( key );
      _waiter?.TrySetResult();
    }
  }

  public Task WaitForNextKeyAsync() {
    if ( !_keyBuffer.IsEmpty )
      return Task.CompletedTask;

    _waiter = new(TaskCreationOptions.RunContinuationsAsynchronously);
    return _waiter.Task;
  }

  public ConsoleKey? ConsumeKey() {
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
}