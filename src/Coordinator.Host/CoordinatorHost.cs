using Drift.Common;
using Drift.Coordinator.Host.Apis.Control;
using Drift.Coordinator.Host.Logging;
using Drift.Coordinator.Host.Ui;
using Drift.Domain.ExecutionEnvironment;
using Drift.Messaging.Protocol.Agent;
using Drift.Networking.Client;
using Drift.Networking.Core;
using Drift.Networking.Server;
using Drift.Scanning;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ControlJsonSerializerContext = Drift.Coordinator.Host.Apis.Control.ControlJsonSerializerContext;

namespace Drift.Coordinator.Host;

// TODO mostly a duplicate of AgentHost
public static class CoordinatorHost {
  public static Task Run(
    ushort controlPort,
    ushort? agentPort,
    ILogger logger,
    Action<IServiceCollection>? configureServices,
    CancellationToken cancellationToken,
    TaskCompletionSource? ready = null
  ) {
    var app = Build( controlPort, agentPort, logger, configureServices, ready );
    return app.RunAsync( cancellationToken );
  }

  public static WebApplication Build(
    ushort controlPort,
    ushort? agentPort,
    ILogger logger,
    Action<IServiceCollection>? configureServices = null,
    TaskCompletionSource? ready = null
  ) {
    var builder = WebApplication.CreateSlimBuilder();

    builder.Services.AddOpenApi( "v1" );
    builder.Services.ConfigureHttpJsonOptions( options =>
      options.SerializerOptions.TypeInfoResolverChain.Insert( 0, ControlJsonSerializerContext.Default )
    );
    builder.Logging.ClearProviders();
    builder.Services.AddSingleton( logger );
    // TODO consolidate all the addmessaging* into single configurable extension that can be used for all roles
    // (CLI, Agent, Coordinator) with different config flags. Should be high-level (domain preferred)
    builder.Services.AddMessagingServer( options => {
      options.EnableDetailedErrors = true;
    } );
    builder.Services.AddMessagingClient();
    var messagingOptions =
      new MessagingOptions { MessageAssembly = typeof(AgentProtocolMessagesAssemblyMarker).Assembly };
    builder.Services.AddMessagingCore( messagingOptions );
    builder.Services.AddScanning();
    builder.Services.AddSingleton<IExecutionEnvironmentProvider, EnvironmentExecutionEnvironmentProvider>();
    builder.Services.AddControlServices();
    configureServices?.Invoke( builder.Services );

    builder.WebHost.ConfigureKestrel( options => {
      if ( agentPort is { } port ) {
        options.ListenAnyIP(
          port,
          o => o.Protocols = HttpProtocols.Http2 // gRPC requires HTTP/2
        );
      }

      options.ListenAnyIP(
        controlPort,
        o => o.Protocols = HttpProtocols.Http1
      );
    } );

    var app = builder.Build();

    app.AddGlobalExceptionHandling( logger );
    app.AddRequestLogging( logger );

    // Note: a service reading StoppingToken during initialization (really, any code run before this point)
    // will get CancellationToken.None.
    messagingOptions.StoppingToken = app.Lifetime.ApplicationStopping;

    app.MapUi();
    app.MapControlApi();
    app.MapMessagingServerEndpoints();
    app.MapOpenApi( "/api/v1/openapi.json" );
    app.MapSwaggerUI( "api", options => {
        options.SwaggerEndpoint( "/api/v1/openapi.json", "Control API v1" );
        options.DocumentTitle = "Drift API";
      }
    );

    app.Lifetime.ApplicationStarted.Register( () => {
      logger.LogInformation( "Control API listening on port {Port} (HTTP)", controlPort );
      if ( agentPort is { } port ) {
        logger.LogInformation( "Listening for inbound agent connections on port {Port} (gRPC)", port );
      }
      else {
        logger.LogInformation(
          "The server is not listening for inbound agent connections. Outbound connections are still possible."
        );
      }

      logger.LogInformation( "Server started" );
      ready?.TrySetResult();
    } );
    app.Lifetime.ApplicationStopping.Register( () => {
      logger.LogInformation( "Server stopping..." );
    } );
    app.Lifetime.ApplicationStopped.Register( () => {
      logger.LogInformation( "Server stopped" );
    } );

    return app;
  }
}