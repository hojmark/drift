using System.Collections.Concurrent;

namespace Drift.Cli.Commands.Scan.Interactive.Input;

internal sealed class KeyWatcher : IAsyncDisposable {
  private readonly ConcurrentQueue<ConsoleKey> _keyBuffer = new();
  private readonly CancellationTokenSource _cts = new();
  private readonly Task _listenerTask;
  private TaskCompletionSource? _waiter;

  public KeyWatcher() {
    _listenerTask = Task.Run( ListenLoopAsync );
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

  private void ListenLoopAsync() {
    while ( !_cts.Token.IsCancellationRequested ) {
      var key = Console.ReadKey( intercept: true ).Key;
      _keyBuffer.Enqueue( key );
      _waiter?.TrySetResult();
    }
  }
}