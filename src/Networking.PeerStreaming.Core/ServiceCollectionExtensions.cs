using System.Reflection;
using Drift.Networking.PeerStreaming.Core.Messages;
using Microsoft.Extensions.DependencyInjection;

namespace Drift.Networking.PeerStreaming.Core;

public static class ServiceCollectionExtensions {
  public static void AddPeerStreamingCore( this IServiceCollection services, Assembly messageAssembly ) {
    services.AddSingleton<IPeerMessageEnvelopeConverter>( new PeerMessageEnvelopeConverter( messageAssembly ) );
    services.AddScoped<PeerMessageDispatcher>();
    services.AddScoped<PeerStreamManager>();
    services.AddScoped<PeerResponseAwaiter>();
  }
}