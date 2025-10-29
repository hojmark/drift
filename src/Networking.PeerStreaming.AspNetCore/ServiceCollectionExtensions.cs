using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Drift.Networking.PeerStreaming.AspNetCore;

public static class ServiceCollectionExtensions {
  public static void AddPeerStreamingServer( this IServiceCollection services ) {
    services.AddGrpc();
  }

  public static void MapPeerStreamingServerEndpoints( this IEndpointRouteBuilder app ) {
    app.MapGrpcService<InboundPeerService>();
  }
}