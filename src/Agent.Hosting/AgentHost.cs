using System.Reflection;
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
    var builder = WebApplication.CreateBuilder();

    builder.Logging.ClearProviders();
    builder.Services.AddSingleton( logger );
    builder.Services.AddGrpc( o => {
      o.EnableDetailedErrors = true;
      o.Interceptors.Add<ExceptionHandlerIntercepter>();
    } );
    builder.Services.AddSingleton<ExceptionHandlerIntercepter>();
    builder.Services.AddPeerStreamingServer();
    builder.Services.AddPeerStreamingClient();
    builder.Services.AddPeerStreamingCore( messageAssembly: Assembly.GetExecutingAssembly() );
    configureServices?.Invoke( builder.Services );

    builder.WebHost.ConfigureKestrel( options => {
      options.ListenLocalhost( port, o => {
        o.Protocols = HttpProtocols.Http2; // Allow HTTP/2 over plain HTTP i.e., non-HTTPS
      } );
    } );

    var app = builder.Build();

    app.MapPeerStreamingServerEndpoints();
    app.MapGet( "/", () => "Nothing to see here" );

    app.Lifetime.ApplicationStarted.Register( () => {
      logger.LogInformation( "Agent started" );
      logger.LogInformation( "Listening for incoming connections on port {Port}", port );
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