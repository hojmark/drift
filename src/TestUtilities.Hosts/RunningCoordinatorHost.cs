using Drift.Coordinator.Host;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Drift.TestUtilities.Hosts;

public sealed class RunningCoordinatorHost : IAsyncDisposable {
  private readonly CancellationTokenSource _cancellation;
  private readonly Task _runTask;
  private int _stopStarted;

  private RunningCoordinatorHost(
    Uri address,
    CancellationTokenSource cancellation,
    Task runTask
  ) {
    Address = address;
    _cancellation = cancellation;
    _runTask = runTask;
  }

  public Uri Address {
    get;
  }

  public static async Task<RunningCoordinatorHost> StartAsync(
    ushort controlPort,
    ushort? agentPort,
    ILogger logger,
    Action<IServiceCollection>? configureServices = null
  ) {
    var cancellation = new CancellationTokenSource();
    var ready = new TaskCompletionSource( TaskCreationOptions.RunContinuationsAsynchronously );
    var address = new Uri( $"http://127.0.0.1:{controlPort}" );
    var host = new RunningCoordinatorHost(
      address,
      cancellation,
      CoordinatorHost.Run( controlPort, agentPort, logger, configureServices, cancellation.Token, ready )
    );

    try {
      await ready.Task.WaitAsync( TimeSpan.FromSeconds( 10 ), cancellation.Token );
      return host;
    }
    catch {
      await host.DisposeAsync();
      throw;
    }
  }

  public HttpClient CreateHttpClient() => new() { BaseAddress = Address };

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