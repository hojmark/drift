using System.Reflection;
using Drift.Networking.PeerStreaming.Inbound;
using Drift.Networking.PeerStreaming.Messages;
using Drift.Networking.PeerStreaming.Outbound;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Drift.Networking.PeerStreaming;

public static class ServiceCollectionExtensions {
  public static void AddPeerStreamingServices( this IServiceCollection services, Assembly ipeermessageassembly ) {
    services.AddSingleton<IPeerClientFactory, DefaultPeerClientFactory>();
    services.AddSingleton<IPeerMessageEnvelopeConverter>( new PeerMessageEnvelopeConverter( ipeermessageassembly ) );
    services.AddScoped<PeerMessageDispatcher>();
    services.AddScoped<PeerStreamManager>();
    services.AddScoped<PeerResponseAwaiter>();
  }

  public static void MapPeerStreamingEndpoints( this IEndpointRouteBuilder app ) {
    app.MapGrpcService<InboundPeerService>();
  }
}