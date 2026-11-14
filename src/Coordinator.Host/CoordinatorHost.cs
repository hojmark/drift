using Drift.Common;
using Drift.Common.IO;
using Drift.Coordinator.Api;
using Drift.Coordinator.Host.Ui;
using Drift.Domain.ExecutionEnvironment;
using Drift.Hosting;
using Drift.Hosting.Configuration;
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
using Microsoft.Extensions.Options;

namespace Drift.Coordinator.Host;

public static class CoordinatorHost {
  /// <summary>
  /// Runs the coordinator host until <paramref name="cancellationToken"/> is canceled.
  /// </summary>
  /// <param name="options">Fully resolved options, e.g. from <see cref="CoordinatorOptions.Bind"/>.</param>
  /// <exception cref="OptionsValidationException"><paramref name="options"/> are invalid.</exception>
  public static Task Run(
    CoordinatorOptions options,
    ILogger logger,
    Action<IServiceCollection>? configureServices,
    CancellationToken cancellationToken,
    TaskCompletionSource? ready = null
  ) {
    OptionsValidation.ThrowIfAny<CoordinatorOptions>( options.Validate() );
    var app = Build( options, logger, configureServices, ready );
    return app.RunAsync( cancellationToken );
  }

  private static WebApplication Build(
    CoordinatorOptions options,
    ILogger logger,
    Action<IServiceCollection>? configureServices = null,
    TaskCompletionSource? ready = null
  ) {
    var builder = WebApplication.CreateSlimBuilder();
    // Options are resolved by the caller. Drop default sources (appsettings.json in the working directory,
    // ASPNETCORE_*/DOTNET_* environment variables) so they can't silently change the host's behavior.
    builder.Configuration.Sources.Clear();

    builder.Logging.ClearProviders();
    builder.Services.AddSingleton( logger );
    builder.Services.AddSingleton( Options.Create( options ) );
    builder.Services.AddSingleton<IExecutionEnvironmentProvider, EnvironmentExecutionEnvironmentProvider>();

    var messagingOptions = new MessagingOptions {
      MessageAssembly = typeof(AgentProtocolMessagesAssemblyMarker).Assembly
    };
    builder.Services.AddMessagingCore( messagingOptions );
    builder.Services.AddMessagingClient();
    if ( options.ListenForAgents ) {
      builder.Services.AddMessagingServer( serverOptions => {
        serverOptions.EnableDetailedErrors = true;
      } );
    }

    builder.Services.AddCoordinatorServices();
    builder.Services.AddCoordinatorApi();

    configureServices?.Invoke( builder.Services );

    builder.WebHost.ConfigureKestrel( kestrel => {
      // Agent gRPC
      if ( options.ListenForAgents ) {
        kestrel.ListenAnyIP(
          options.AgentPort,
          o => o.Protocols = HttpProtocols.Http2 // gRPC requires HTTP/2
        );
      }

      // UI HTTP
      kestrel.ListenAnyIP(
        options.Port,
        o => o.Protocols = HttpProtocols.Http1
      );
    } );

    var app = builder.Build();

    // Note: code reading StoppingToken before this point will get CancellationToken.None
    messagingOptions.StoppingToken = app.Lifetime.ApplicationStopping;

    app.Services.GetRequiredService<ICoordinatorDataLocation>().EnsureCreated();

    app.AddGlobalExceptionHandling( logger );
    if ( options.RequestLogging.Enabled ) {
      app.AddRequestLogging( logger );
    }

    app.MapUi();
    app.MapCoordinatorApi();
    if ( options.ListenForAgents ) {
      app.MapMessagingServerEndpoints();
    }

    app.MapOpenApi( "/api/v1/openapi.json" );
    app.MapSwaggerUI( "api", swagger => {
        swagger.SwaggerEndpoint( "/api/v1/openapi.json", "Control API v1" );
        swagger.DocumentTitle = "Drift API";
      }
    );

    app.Lifetime.ApplicationStarted.Register( () => {
      logger.LogDebug(
        "Coordinator data directory: {DataDirectory}",
        app.Services.GetRequiredService<ICoordinatorDataLocation>().Directory
      );
      logger.LogInformation( "Control API port: {Port} (HTTP)", options.Port );
      if ( options.ListenForAgents ) {
        logger.LogInformation( "Agent port: {Port} (gRPC)", options.AgentPort );
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