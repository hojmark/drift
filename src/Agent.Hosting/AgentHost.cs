using Drift.Agent.PeerProtocol.Subnets;
using Drift.Networking.PeerStreaming.Client;
using Drift.Networking.PeerStreaming.Core;
using Drift.Networking.PeerStreaming.Server;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Drift.Agent.Hosting;

public static class AgentHost {
  public static Task Run(
    ushort port,
    ILogger logger,
    Action<IServiceCollection>? configureServices,
    CancellationToken cancellationToken
  ) {
    var app = Build( port, logger, configureServices );
    return app.RunAsync( cancellationToken );
  }

  private static WebApplication Build(
    ushort port,
    ILogger logger,
    Action<IServiceCollection>? configureServices = null
  ) {
    var builder = WebApplication.CreateSlimBuilder();

    builder.Logging.ClearProviders();
    builder.Services.AddSingleton( logger );
    builder.Services.AddPeerStreamingServer( options => {
      options.EnableDetailedErrors = true;
    } );
    builder.Services.AddPeerStreamingClient();
    var peerStreamingOptions = new PeerStreamingOptions { MessageAssembly = typeof(SubnetsRequest).Assembly };
    builder.Services.AddPeerStreamingCore( peerStreamingOptions );
    configureServices?.Invoke( builder.Services );

    builder.WebHost.ConfigureKestrel( options => {
      options.ListenLocalhost( port, o => {
        o.Protocols = HttpProtocols.Http2; // Allow HTTP/2 over plain HTTP i.e., non-HTTPS
      } );
    } );

    var app = builder.Build();

    peerStreamingOptions.StoppingToken = app.Lifetime.ApplicationStopping;

    app.MapPeerStreamingServerEndpoints();
    app.MapGet( "/", () => "Nothing to see here" );

    app.Lifetime.ApplicationStarted.Register( () => {
      logger.LogInformation( "Listening for incoming connections on port {Port}", port );
      logger.LogInformation( "Agent started" );
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