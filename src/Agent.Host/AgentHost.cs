using Drift.Common;
using Drift.Common.IO;
using Drift.Domain.ExecutionEnvironment;
using Drift.Hosting;
using Drift.Hosting.Configuration;
using Drift.Messaging.Protocol.Agent;
using Drift.Networking.Client;
using Drift.Networking.Core;
using Drift.Networking.Server;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Drift.Agent.Host;

public static class AgentHost {
  /// <summary>
  /// Runs the agent host until <paramref name="cancellationToken"/> is canceled.
  /// </summary>
  /// <param name="options">Fully resolved options, e.g. from <see cref="AgentOptions.Bind"/>.</param>
  /// <exception cref="OptionsValidationException"><paramref name="options"/> are invalid.</exception>
  public static Task Run(
    AgentOptions options,
    ILogger logger,
    Action<IServiceCollection>? configureServices,
    CancellationToken cancellationToken,
    TaskCompletionSource? ready = null
  ) {
    OptionsValidation.ThrowIfAny<AgentOptions>( options.Validate() );
    var app = Build( options, logger, configureServices, ready );
    return app.RunAsync( cancellationToken );
  }

  private static WebApplication Build(
    AgentOptions options,
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
    if ( options.Listen ) {
      builder.Services.AddMessagingServer( serverOptions => {
        serverOptions.EnableDetailedErrors = true;
      } );
    }

    builder.Services.AddAgentServices();

    configureServices?.Invoke( builder.Services );

    if ( options.Listen ) {
      builder.WebHost.ConfigureKestrel( kestrel => {
        kestrel.ListenAnyIP(
          options.Port,
          o => o.Protocols = HttpProtocols.Http2 // gRPC requires HTTP/2
        );
      } );
    }
    else {
      // Without explicit endpoints, Kestrel would bind its default address
      builder.Services.AddSingleton<IServer, NoopServer>();
    }

    var app = builder.Build();

    // Note: code reading StoppingToken before this point will get CancellationToken.None
    messagingOptions.StoppingToken = app.Lifetime.ApplicationStopping;

    app.Services.GetRequiredService<IAgentDataLocation>().EnsureCreated();

    if ( options.RequestLogging.Enabled ) {
      app.AddRequestLogging( logger );
    }

    if ( options.Listen ) {
      app.MapMessagingServerEndpoints();
    }

    app.Lifetime.ApplicationStarted.Register( () => {
      logger.LogDebug(
        "Agent data directory: {DataDirectory}",
        app.Services.GetRequiredService<IAgentDataLocation>().Directory
      );
      if ( options.Listen ) {
        logger.LogInformation( "Port: {Port} (gRPC)", options.Port );
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