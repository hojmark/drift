using Drift.Agent.Host;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Drift.TestUtilities.Hosts;

public sealed class RunningAgentHost : IAsyncDisposable {
  private readonly CancellationTokenSource _cancellation;
  private readonly Task _runTask;
  private int _stopStarted;

  private RunningAgentHost( ushort port, CancellationTokenSource cancellation, Task runTask ) {
    Port = port;
    _cancellation = cancellation;
    _runTask = runTask;
  }

  public ushort Port {
    get;
  }

  public static async Task<RunningAgentHost> StartAsync(
    ushort port,
    ILogger logger,
    Action<IServiceCollection>? configureServices = null
  ) {
    var cancellation = new CancellationTokenSource();
    var ready = new TaskCompletionSource( TaskCreationOptions.RunContinuationsAsynchronously );
    var host = new RunningAgentHost(
      port,
      cancellation,
      AgentHost.Run( port, logger, configureServices, cancellation.Token, ready )
    );

    try {
      await ready.Task.WaitAsync( TimeSpan.FromSeconds( 10 ) );
      return host;
    }
    catch {
      await host.DisposeAsync();
      throw;
    }
  }

  public ValueTask DisposeAsync() => new(StopAsync());

  public async Task StopAsync() {
    if ( Interlocked.Exchange( ref _stopStarted, 1 ) != 0 ) {
      return;
    }

    await _cancellation.CancelAsync();
    try {
      await _runTask;
    }
    catch ( OperationCanceledException ) when ( _cancellation.IsCancellationRequested ) {
      // Expected when stopping the host.
    }
    finally {
      _cancellation.Dispose();
    }
  }
}