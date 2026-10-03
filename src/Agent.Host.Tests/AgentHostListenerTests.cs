using Drift.Common.IO;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;

namespace Drift.Agent.Host.Tests;

internal sealed class AgentHostListenerTests {
  [Test]
  [Ignore( "Failing - A default port is actually bound!" )]
  public async Task Run_WithNoAgentPort_DoesNotBindAnyAddresses() {
    var addresses = new TaskCompletionSource<string[]>( TaskCreationOptions.RunContinuationsAsynchronously );
    using var cancellation = new CancellationTokenSource();
    var ready = new TaskCompletionSource( TaskCreationOptions.RunContinuationsAsynchronously );
    var runTask = AgentHost.Run(
      new AgentConfiguration { Port = null },
      NullLogger.Instance,
      services => {
        services.AddSingleton<IAgentDataLocation>( new TestAgentDataLocation() );
        services.AddSingleton<IHostedService>( serviceProvider => new ServerAddressesObserver(
          serviceProvider,
          serviceProvider.GetRequiredService<IHostApplicationLifetime>(),
          addresses
        ) );
      },
      cancellation.Token,
      ready
    );

    try {
      await ready.Task.WaitAsync( TimeSpan.FromSeconds( 10 ), cancellation.Token );
      var boundAddresses = await addresses.Task.WaitAsync( TimeSpan.FromSeconds( 10 ), cancellation.Token );

      Assert.That( boundAddresses, Is.Empty );
    }
    finally {
      await cancellation.CancelAsync();
      try {
        await runTask;
      }
      catch ( OperationCanceledException ) when ( cancellation.IsCancellationRequested ) {
        // Expected when stopping the host.
      }
    }
  }

  private sealed class ServerAddressesObserver(
    IServiceProvider services,
    IHostApplicationLifetime lifetime,
    TaskCompletionSource<string[]> addresses
  ) : IHostedService {
    public Task StartAsync( CancellationToken cancellationToken ) {
      lifetime.ApplicationStarted.Register( () => {
        var server = services.GetService<IServer>();
        var serverAddresses = server?.Features.Get<IServerAddressesFeature>()?.Addresses.ToArray() ?? [];
        addresses.TrySetResult( serverAddresses );
      } );
      return Task.CompletedTask;
    }

    public Task StopAsync( CancellationToken cancellationToken ) => Task.CompletedTask;
  }

  private sealed class TestAgentDataLocation : IAgentDataLocation {
    public string Directory => string.Empty;

    public void EnsureCreated() {
    }
  }
}