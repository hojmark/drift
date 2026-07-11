using Drift.Agent.Host.Scan;
using Drift.Agent.Host.Subnets;
using Drift.Networking.Core.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Drift.Agent.Host;

public static class ServiceCollectionExtensions {
  extension( IServiceCollection services ) {
    public void AddAgentHandlers() {
      services.AddScoped<IMessageHandler, SubnetsRequestHandler>();
      services.AddScoped<IMessageHandler, ScanSubnetRequestHandler>();
    }
  }
}
