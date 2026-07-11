using Drift.Networking.Core.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Drift.Networking.Client;

public static class ServiceCollectionExtensions {
  public static void AddMessagingClient( this IServiceCollection services ) {
    services.AddSingleton<IMessagingClientFactory, DefaultMessagingClientFactory>();
  }
}