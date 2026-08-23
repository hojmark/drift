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

namespace Drift.Coordinator.Host;

// TODO mostly a duplicate of AgentHost
public static class CoordinatorHost {
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

  public static WebApplication Build(
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
        o.Protocols = HttpProtocols.Http2; // gRPC requires HTTP/2
      } );
    } );

    AddServerStuff( builder );

    var app = builder.Build();

    // Note: a service reading StoppingToken during initialization (really, any code run before this point)
    // will get CancellationToken.None.
    messagingOptions.StoppingToken = app.Lifetime.ApplicationStopping;

    // Unreachable while Kestrel ListenOptions.Protocols is HTTP/2-only (browsers can't speak HTTP/2 without TLS),
    // but kept here for when this is moved to its own HTTP/1.1 port.
    // Setting it to Http1AndHttp2 is not an option since that degrades ALL connections, including gRPC
    // calls, to HTTP/1.1, which then fail against gRPC's HTTP/2-only endpoints with HTTP_1_1_REQUIRED.
    // See https://github.com/grpc/grpc-dotnet/issues/979. So this must stay HTTP/2-only until either
    // TLS is added or the friendly "/" page below is moved to its own HTTP/1.1-only port.
    // app.MapGet( "/", () =>
    //   // TODO Render figlet using same flf as in the help command
    //   """
    //     ___          _    __   _
    //    |   \   _ _  (_)  / _| | |_
    //    | |) | | '_| | | |  _| |  _|
    //    |___/  |_|   |_| |_|    \__|
    //   """
    //   +
    //   "\n\Server"
    // );
    app.MapMessagingServerEndpoints();

    app.Lifetime.ApplicationStarted.Register( () => {
      logger.LogInformation( "Listening for incoming connections on port {Port}", port );
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

  private static void AddServerStuff( WebApplicationBuilder app ) {
  }
}