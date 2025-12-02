using Drift.Agent.PeerProtocol.Subnets;
using Drift.Networking.PeerStreaming.Core.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Drift.Agent.PeerProtocol;

public static class ServiceCollectionExtensions {
  extension( IServiceCollection services ) {
    public void AddPeerProtocol() {
      services.AddScoped<IPeerMessageHandler, SubnetsRequestHandler>();
    }
  }
}