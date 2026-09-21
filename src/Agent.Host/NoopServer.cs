using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http.Features;

namespace Drift.Agent.Host;

/// <summary>
/// An <see cref="IServer"/> that does not do anything.
/// </summary>
internal sealed class NoopServer : IServer {
  public IFeatureCollection Features {
    get;
  } = new FeatureCollection();

  public Task StartAsync<TContext>( IHttpApplication<TContext> application, CancellationToken cancellationToken )
    where TContext : notnull {
    return Task.CompletedTask;
  }

  public Task StopAsync( CancellationToken cancellationToken ) {
    return Task.CompletedTask;
  }

  public void Dispose() {
  }
}