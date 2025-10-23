using System.Reflection;
using Drift.Networking.Grpc;
using Drift.Networking.Grpc.Messages;
using Microsoft.Extensions.DependencyInjection;

namespace Drift.Networking.Peer;

public static class ServiceCollectionExtensions {
  public static void AddPeerServices( this IServiceCollection services, Assembly ipeermessageassembly ) {
    services.AddSingleton<IPeerMessageSerializer>( new PeerMessageSerializer( ipeermessageassembly ) );
    services.AddMessageHandling();
    services.AddSingleton<IPeerClientFactory, DefaultPeerClientFactory>();
    // services.AddScoped<IPeerMessageHandler, GiveMeSubnetsHandler>();
    services.AddScoped<PeerStreamManager>();
  }
}