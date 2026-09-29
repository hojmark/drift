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

// TODO mostly a duplicate of AgentHostse
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

    builder.Logging.ClearProviders();
    builder.Services.AddSingleton( logger );
    builder.Services.AddSingleton<IExecutionEnvironmentProvider, EnvironmentExecutionEnvironmentProvider>();

    // TODO consolidate all the addmessaging* into single configurable extension that can be used for all roles
    // (CLI, Agent, Coordinator) with different config flags. Should be high-level (domain preferred)
    if ( agentPort is not null ) {
      builder.Services.AddMessagingServer( options => {
        options.EnableDetailedErrors = true;
      } );
    }

    builder.Services.AddMessagingClient();
    var messagingOptions = new MessagingOptions {
      MessageAssembly = typeof(AgentProtocolMessagesAssemblyMarker).Assembly
    };
    builder.Services.AddMessagingCore( messagingOptions );

    builder.Services.AddCoordinatorServices();
    builder.Services.AddCoordinatorApi();

    configureServices?.Invoke( builder.Services );

    builder.WebHost.ConfigureKestrel( options => {
      // Agent gRPC
      if ( agentPort is not null ) {
        options.ListenAnyIP(
          agentPort.Value,
          o => o.Protocols = HttpProtocols.Http2 // gRPC requires HTTP/2
        );
      }

      // UI HTTP
      options.ListenAnyIP(
        controlPort,
        o => o.Protocols = HttpProtocols.Http1
      );
    } );

    var app = builder.Build();

    // Note: code reading StoppingToken before this point will get CancellationToken.None
    messagingOptions.StoppingToken = app.Lifetime.ApplicationStopping;

    app.Services.GetRequiredService<ICoordinatorDataLocation>().EnsureCreated();

    app.AddGlobalExceptionHandling( logger );
    app.AddRequestLogging( logger );

    app.MapUi();
    app.MapCoordinatorApi();
    if ( agentPort is not null ) {
      app.MapMessagingServerEndpoints();
    }

    app.MapOpenApi( "/api/v1/openapi.json" );
    app.MapSwaggerUI( "api", options => {
        options.SwaggerEndpoint( "/api/v1/openapi.json", "Control API v1" );
        options.DocumentTitle = "Drift API";
      }
    );

    /*logger.LogInformation( "\n" +
                           $"""
                             ___          _    __   _
                            |   \   _ _  (_)  / _| | |_
                            | |) | | '_| | | |  _| |  _|
                            |___/  |_|   |_| |_|    \__|
                            Agent

                            Version: 1.2.3
                            API docs: /api
                            """
    );*/

    app.Lifetime.ApplicationStarted.Register( () => {
      logger.LogDebug(
        "Coordinator data directory: {DataDirectory}",
        app.Services.GetRequiredService<ICoordinatorDataLocation>().Directory
      );
      logger.LogInformation( "Control API listening on port {Port} (HTTP)", controlPort );
      if ( agentPort is { } port ) {
        logger.LogInformation( "Listening for inbound agent connections on port {Port} (gRPC)", port );
      }
      else {
        logger.LogWarning(
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