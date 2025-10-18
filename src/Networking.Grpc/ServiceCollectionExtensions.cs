using Drift.Networking.Grpc.Messages;
using Drift.Networking.Grpc.Messages.Adopt;
using Microsoft.Extensions.DependencyInjection;

namespace Drift.Networking.Grpc;

public static class ServiceCollectionExtensions {
  public static void AddHandling( this IServiceCollection services ) {
    services.AddScoped<PeerMessageHandlerDispatcher>();

    services.AddScoped<IPeerMessageHandler, AdoptRequestHandler>();
    //services.AddScoped<IPeerMessageHandler, AdoptResponseHandler>();
  }
}