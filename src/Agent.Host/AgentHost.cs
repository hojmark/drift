using Drift.Common;
using Drift.Common.IO;
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

namespace Drift.Agent.Host;

public static class AgentHost {
  public static Task Run(
    AgentConfiguration configuration,
    ILogger logger,
    Action<IServiceCollection>? configureServices,
    CancellationToken cancellationToken,
    TaskCompletionSource? ready = null
  ) {
    var app = Build( configuration, logger, configureServices, ready );
    return app.RunAsync( cancellationToken );
  }

  private static WebApplication Build(
    AgentConfiguration configuration,
    ILogger logger,
    Action<IServiceCollection>? configureServices = null,
    TaskCompletionSource? ready = null
  ) {
    var builder = WebApplication.CreateSlimBuilder();

    builder.Logging.ClearProviders();
    builder.Services.AddSingleton( logger );
    builder.Services.AddSingleton<IExecutionEnvironmentProvider, EnvironmentExecutionEnvironmentProvider>();

    if ( configuration.Port is not null ) {
      builder.Services.AddMessagingServer( options => {
        options.EnableDetailedErrors = true;
      } );
    }

    builder.Services.AddMessagingClient();
    var messagingOptions = new MessagingOptions {
      MessageAssembly = typeof(AgentProtocolMessagesAssemblyMarker).Assembly
    };
    builder.Services.AddMessagingCore( messagingOptions );

    builder.Services.AddAgentServices();

    configureServices?.Invoke( builder.Services );

    builder.WebHost.ConfigureKestrel( options => {
      if ( configuration.Port is not null ) {
        options.ListenAnyIP(
          configuration.Port.Value,
          o => o.Protocols = HttpProtocols.Http2 // gRPC requires HTTP/2
        );
      }
    } );

    var app = builder.Build();

    // Note: code reading StoppingToken before this point will get CancellationToken.None
    messagingOptions.StoppingToken = app.Lifetime.ApplicationStopping;

    app.Services.GetRequiredService<IAgentDataLocation>().EnsureCreated();

    if ( configuration.Port is not null ) {
      app.MapMessagingServerEndpoints();
    }

    app.Lifetime.ApplicationStarted.Register( () => {
      logger.LogDebug(
        "Agent data directory: {DataDirectory}",
        app.Services.GetRequiredService<IAgentDataLocation>().Directory
      );
      if ( configuration.Port is not null ) {
        logger.LogInformation( "Port: {Port} (gRPC)", configuration.Port.Value );
      }
      else {
        logger.LogWarning(
          "Not listening for inbound server connections. Outbound connections are still possible."
        );
      }

      logger.LogInformation( "Agent started" );
      ready?.TrySetResult();
    } );
    app.Lifetime.ApplicationStopping.Register( () => {
      logger.LogInformation( "Agent stopping..." );
    } );
    app.Lifetime.ApplicationStopped.Register( () => {
      logger.LogInformation( "Agent stopped" );
    } );

    logger.LogInformation(
      """

       ___          _    __   _
      |   \   _ _  (_)  / _| | |_
      | |) | | '_| | | |  _| |  _|
      |___/  |_|   |_| |_|    \__| AGENT 


      """
    );
    logger.LogInformation( "Version: {Version}", DriftMetadata.Version );

    return app;
  }
}