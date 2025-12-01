using Drift.Agent.PeerProtocol.Subnets;
using Drift.Networking.PeerStreaming.Core.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Drift.Agent.PeerProtocol;

public static class ServiceCollectionExtensions {
  public static void AddPeerProtocol( this IServiceCollection services ) {
    //TODO need both?
    // services.AddScoped<IPeerMessageHandlerBase, SubnetsRequestHandler>();
    // services.AddScoped<IPeerMessageHandler<SubnetsRequest, SubnetsResponse>, SubnetsRequestHandler>();
    services.AddPeerMessageHandler<SubnetsRequestHandler, SubnetsRequest, SubnetsResponse>();

    //services.AddScoped<IPeerMessageTypesProvider, PeerProtocolTypesProvider>();
  }

  private static IServiceCollection AddPeerMessageHandler<THandler, TReq, TRes>( this IServiceCollection services )
    where THandler : class, IPeerMessageHandler<TReq, TRes>
    where TReq : IPeerMessage
    where TRes : IPeerMessage {
    services.AddScoped<IPeerMessageHandlerBase, THandler>();
    services.AddScoped<IPeerMessageHandler<TReq, TRes>, THandler>();
    return services;
  }
}