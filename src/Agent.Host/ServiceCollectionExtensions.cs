using Drift.Agent.Host.Scan;
using Drift.Agent.Host.Status;
using Drift.Agent.Host.Subnets;
using Drift.Networking.Core.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Drift.Agent.Host;

public static class ServiceCollectionExtensions {
  extension( IServiceCollection services ) {
    public void AddAgentHandlers() {
      services.AddScoped<IMessageHandler, InterfaceSubnetsRequestHandler>();
      services.AddScoped<IMessageHandler, ScanSubnetRequestHandler>();
      services.AddScoped<IMessageHandler, AgentStatusRequestHandler>();
    }
  }
}
