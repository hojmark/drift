using Drift.Agent.PeerProtocol.Subnets;
using Drift.Networking.PeerStreaming.Core.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Drift.Agent.PeerProtocol;

public static class ServiceCollectionExtensions {
  public static void AddPeerProtocol( this IServiceCollection services ) {
    //TODO need both?
    services.AddScoped<IPeerMessageHandler<SubnetsRequest>, SubnetsRequestHandler>();
    services.AddScoped<IPeerMessageHandler, SubnetsRequestHandler>();
  }
}