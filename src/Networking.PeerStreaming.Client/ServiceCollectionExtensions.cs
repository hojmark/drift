using Drift.Networking.PeerStreaming.Core;
using Microsoft.Extensions.DependencyInjection;

namespace Drift.Networking.PeerStreaming.Client;

public static class ServiceCollectionExtensions {
  public static void AddPeerStreamingClient( this IServiceCollection services ) {
    services.AddSingleton<IPeerClientFactory, DefaultPeerClientFactory>();
  }
}