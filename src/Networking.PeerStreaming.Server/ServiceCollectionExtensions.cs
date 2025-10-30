using Grpc.AspNetCore.Server;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Drift.Networking.PeerStreaming.Server;

public static class ServiceCollectionExtensions {
  public static void AddPeerStreamingServer(
    this IServiceCollection services,
    Action<GrpcServiceOptions>? configureOptions = null
  ) {
    services.AddSingleton<PeerStreamingServerMarker>();
    services.AddGrpc( options => configureOptions?.Invoke( options ) );
    services.AddTransient<IStartupFilter, PeerStreamingServerValidationFilter>();
  }

  public static void MapPeerStreamingServerEndpoints( this IEndpointRouteBuilder app ) {
    var marker = app.ServiceProvider.GetService<PeerStreamingServerMarker>();

    if ( marker == null ) {
      throw new InvalidOperationException(
        $"Unable to find the required services. Add them by calling '{nameof(IServiceCollection)}.{nameof(AddPeerStreamingServer)}'."
      );
    }

    app.MapGrpcService<InboundPeerService>();

    marker.EndpointsMapped = true;
  }
}

internal sealed class PeerStreamingServerValidationFilter : IStartupFilter {
  public Action<IApplicationBuilder> Configure( Action<IApplicationBuilder> next ) {
    return app => {
      next( app );

      var marker = app.ApplicationServices.GetRequiredService<PeerStreamingServerMarker>();

      if ( !marker.EndpointsMapped ) {
        throw new InvalidOperationException(
          $"Server endpoints were not mapped. Map them by calling '{nameof(IEndpointRouteBuilder)}.{nameof(ServiceCollectionExtensions.MapPeerStreamingServerEndpoints)}'." );
      }
    };
  }
}