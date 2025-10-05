using Drift.Networking.PeerStreaming.Core.Abstractions;
using Drift.Networking.PeerStreaming.Core.Messages;
using Microsoft.Extensions.DependencyInjection;

namespace Drift.Networking.PeerStreaming.Core;

public static class ServiceCollectionExtensions {
  public static void AddPeerStreamingCore(
    this IServiceCollection services,
    PeerStreamingOptions options
  ) {
    services.AddSingleton( options );
    services.AddSingleton<IPeerMessageEnvelopeConverter>(
      new PeerMessageEnvelopeConverter(
        new AssemblyScanPeerMessageTypesProvider( options.MessageAssembly )
      )
    );
    services.AddScoped<PeerMessageDispatcher>();
    services.AddScoped<PeerResponseCorrelator>();
    services.AddScoped<IPeerStreamManager, PeerStreamManager>();
  }
}