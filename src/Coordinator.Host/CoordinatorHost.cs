using Drift.Common;
using Drift.Common.IO;
using Drift.Coordinator.Api;
using Drift.Coordinator.Host.Logging;
using Drift.Coordinator.Host.Ui;
using Drift.Domain.ExecutionEnvironment;
using Drift.Messaging.Protocol.Agent;
using Drift.Networking.Client;
using Drift.Networking.Core;
using Drift.Networking.Server;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Drift.Coordinator.Host;

public static class CoordinatorHost {
  public static Task Run(
    CoordinatorConfiguration configuration,
    ILogger logger,
    Action<IServiceCollection>? configureServices,
    CancellationToken cancellationToken,
    TaskCompletionSource? ready = null
  ) {
    var app = Build( configuration, logger, configureServices, ready );
    return app.RunAsync( cancellationToken );
  }

  private static WebApplication Build(
    CoordinatorConfiguration configuration,
    ILogger logger,
    Action<IServiceCollection>? configureServices = null,
    TaskCompletionSource? ready = null
  ) {
    var builder = WebApplication.CreateSlimBuilder();

    builder.Logging.ClearProviders();
    builder.Services.AddSingleton( logger );
    builder.Services.AddSingleton<IExecutionEnvironmentProvider, EnvironmentExecutionEnvironmentProvider>();

    var messagingOptions = new MessagingOptions {
      MessageAssembly = typeof(AgentProtocolMessagesAssemblyMarker).Assembly
    };
    builder.Services.AddMessagingCore( messagingOptions );
    builder.Services.AddMessagingClient();
    if ( configuration.AgentPort is not null ) {
      builder.Services.AddMessagingServer( options => {
        options.EnableDetailedErrors = true;
      } );
    }

    builder.Services.AddCoordinatorServices();
    builder.Services.AddCoordinatorApi();

    configureServices?.Invoke( builder.Services );

    builder.WebHost.ConfigureKestrel( options => {
      // Agent gRPC
      if ( configuration.AgentPort is not null ) {
        options.ListenAnyIP(
          configuration.AgentPort.Value,
          o => o.Protocols = HttpProtocols.Http2 // gRPC requires HTTP/2
        );
      }

      // UI HTTP
      options.ListenAnyIP(
        configuration.Port,
        o => o.Protocols = HttpProtocols.Http1
      );
    } );

    var app = builder.Build();

    // Note: code reading StoppingToken before this point will get CancellationToken.None
    messagingOptions.StoppingToken = app.Lifetime.ApplicationStopping;

    app.Services.GetRequiredService<ICoordinatorDataLocation>().EnsureCreated();

    app.AddGlobalExceptionHandling( logger );
    if ( configuration.EnableRequestLogging ) {
      app.AddRequestLogging( logger );
    }

    app.MapUi();
    app.MapCoordinatorApi();
    if ( configuration.AgentPort is not null ) {
      app.MapMessagingServerEndpoints();
    }

    app.MapOpenApi( "/api/v1/openapi.json" );
    app.MapSwaggerUI( "api", options => {
        options.SwaggerEndpoint( "/api/v1/openapi.json", "Control API v1" );
        options.DocumentTitle = "Drift API";
      }
    );

    app.Lifetime.ApplicationStarted.Register( () => {
      logger.LogDebug(
        "Coordinator data directory: {DataDirectory}",
        app.Services.GetRequiredService<ICoordinatorDataLocation>().Directory
      );
      logger.LogInformation( "Control API port: {Port} (HTTP)", configuration.Port );
      if ( configuration.AgentPort is not null ) {
        logger.LogInformation( "Agent port: {Port} (gRPC)", configuration.AgentPort.Value );
      }
      else {
        logger.LogWarning(
          "Not listening for inbound agent connections. Outbound connections are still possible."
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

    logger.LogInformation(
      """

       ___          _    __   _
      |   \   _ _  (_)  / _| | |_
      | |) | | '_| | | |  _| |  _|
      |___/  |_|   |_| |_|    \__| SERVER 


      """
    );
    logger.LogInformation( "Version: {Version}", DriftMetadata.Version );

    return app;
  }
}