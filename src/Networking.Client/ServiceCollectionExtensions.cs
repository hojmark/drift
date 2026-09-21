using Drift.Networking.Core.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Drift.Networking.Client;

public static class ServiceCollectionExtensions {
  /// <summary>
  /// Registers the services required to create <i>outbound</i> gRPC messaging connections.
  /// </summary>
  public static void AddMessagingClient( this IServiceCollection services ) {
    services.AddSingleton<IMessagingClientFactory, DefaultMessagingClientFactory>();
  }
}