using Drift.Common.IO;
using Drift.Coordinator.Services.Agents;
using Drift.Coordinator.Services.Agents.Enrollment;
using Drift.Coordinator.Services.Scans;
using Drift.Coordinator.Services.Spec;
using Drift.Coordinator.Services.State;
using Drift.Messaging.Client;
using Drift.Scanning;
using Microsoft.Extensions.DependencyInjection;

namespace Drift.Coordinator.Host;

internal static class ServiceCollectionExtensions {
  internal static void AddCoordinatorServices( this IServiceCollection services ) {
    services.AddScanning();

    services.AddSingleton<IDriftDataLocation, DefaultDriftDataLocation>();
    services.AddSingleton<ICoordinatorDataLocation, DefaultCoordinatorDataLocation>();

    services.AddSingleton<ICoordinatorSpecStore, FileCoordinatorSpecStore>();
    services.AddSingleton<CoordinatorSpec>();
    services.AddSingleton<IAgentEnrollmentStore, FileAgentEnrollmentStore>();
    services.AddSingleton<IAgentDirectory>( serviceProvider => {
      var enrollmentStore = serviceProvider.GetRequiredService<IAgentEnrollmentStore>();
      var initialAgents = enrollmentStore.Load()
        .Select( enrollment => new EnrolledAgent(
          enrollment.Id,
          new Uri( enrollment.Address ),
          enrollment.EnrolledAt
        ) )
        .ToArray();
      return new InMemoryAgentDirectory( initialAgents );
    } );
    services.AddSingleton<AgentGateway>();

    services.AddSingleton<AgentManagementService>();
    services.AddSingleton<SpecService>();
    services.AddSingleton<ScanService>();

    services.AddAgentClient();
  }
}