using Microsoft.Extensions.DependencyInjection;

namespace Drift.Messaging.Client;

public static class ServiceCollectionExtensions {
  public static void AddAgentClient( this IServiceCollection services ) {
    services.AddSingleton<IAgentClient, AgentClient>();
  }
}