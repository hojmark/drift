using Drift.Networking.Grpc.Messages;
using Microsoft.Extensions.DependencyInjection;

namespace Drift.Networking.Grpc;

public static class ServiceCollectionExtensions {
  public static void AddMessageHandling( this IServiceCollection services ) {
    services.AddScoped<PeerMessageDispatcher>();
  }
}