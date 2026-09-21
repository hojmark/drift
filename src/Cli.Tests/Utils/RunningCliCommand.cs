namespace Drift.Cli.Tests.Utils;

internal sealed class RunningCliCommand : IAsyncDisposable {
  private readonly CancellationTokenSource _cts;

  internal RunningCliCommand( Task<CliCommandResult> task, CancellationTokenSource cts ) {
    Completion = task;
    _cts = cts;
  }

  public Task<CliCommandResult> Completion {
    get;
  }

  public async ValueTask DisposeAsync() {
    await _cts.CancelAsync();
    await Completion;
    _cts.Dispose();
  }
}