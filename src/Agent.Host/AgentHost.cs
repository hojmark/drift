using Drift.Messaging.Protocol;
using Drift.Networking.Client;
using Drift.Networking.Core;
using Drift.Networking.Server;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Drift.Agent.Host;

public static class AgentHost {
  public static Task Run(
    ushort port,
    ILogger logger,
    Action<IServiceCollection>? configureServices,
    CancellationToken cancellationToken,
    TaskCompletionSource? ready = null
  ) {
    var app = Build( port, logger, configureServices, ready );
    return app.RunAsync( cancellationToken );
  }

  private static WebApplication Build(
    ushort port,
    ILogger logger,
    Action<IServiceCollection>? configureServices = null,
    TaskCompletionSource? ready = null
  ) {
    var builder = WebApplication.CreateSlimBuilder();

    builder.Logging.ClearProviders();
    builder.Services.AddSingleton( logger );
    // TODO consolidate all the addmessaging* into single configurable extension that can be used for all roles
    // (CLI, Agent, Coordinator) with different config flags. Should be high-level (domain preferred)
    builder.Services.AddMessagingServer( options => {
      options.EnableDetailedErrors = true;
    } );
    builder.Services.AddMessagingClient();
    var messagingOptions = new MessagingOptions { MessageAssembly = typeof(ProtocolMessagesAssemblyMarker).Assembly };
    builder.Services.AddMessagingCore( messagingOptions );
    configureServices?.Invoke( builder.Services );

    builder.WebHost.ConfigureKestrel( options => {
      options.ListenAnyIP( port, o => {
        o.Protocols = HttpProtocols.Http2; // Allow HTTP/2 over plain HTTP i.e., non-HTTPS
      } );
    } );

    var app = builder.Build();

    // Note: a service reading StoppingToken during initialization (really, any code run before this point)
    // will get CancellationToken.None.
    messagingOptions.StoppingToken = app.Lifetime.ApplicationStopping;

    app.MapMessagingServerEndpoints();

    app.Lifetime.ApplicationStarted.Register( () => {
      logger.LogInformation( "Listening for incoming connections on port {Port}", port );
      logger.LogInformation( "Agent started" );
      ready?.TrySetResult();
    } );
    app.Lifetime.ApplicationStopping.Register( () => {
      logger.LogInformation( "Agent stopping..." );
    } );
    app.Lifetime.ApplicationStopped.Register( () => {
      logger.LogInformation( "Agent stopped" );
    } );

    return app;
  }
}