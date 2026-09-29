using Drift.Agent.Host.Scan;
using Drift.Agent.Host.Status;
using Drift.Agent.Host.Subnets;
using Drift.Common.IO;
using Drift.Networking.Core.Abstractions;
using Drift.Scanning;
using Microsoft.Extensions.DependencyInjection;

namespace Drift.Agent.Host;

internal static class ServiceCollectionExtensions {
  extension( IServiceCollection services ) {
    internal void AddAgentServices() {
      services.AddScanning();

      services.AddScoped<IMessageHandler, InterfaceSubnetsRequestHandler>();
      services.AddScoped<IMessageHandler, ScanSubnetRequestHandler>();
      services.AddScoped<IMessageHandler, AgentStatusRequestHandler>();

      services.AddSingleton<IDriftDataLocation, DefaultDriftDataLocation>();
      services.AddSingleton<IAgentDataLocation, DefaultAgentDataLocation>();
    }
  }
}