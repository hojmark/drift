using Drift.Common.IO;
using Drift.Coordinator.Services.Agents;
using Drift.Coordinator.Services.Agents.Enrollment;
using Drift.Coordinator.Services.Scans;
using Drift.Coordinator.Services.Spec;
using Drift.Coordinator.Services.State;
using Microsoft.Extensions.DependencyInjection;

namespace Drift.Coordinator.Host;

public static class ServiceCollectionExtensions {
  /// <summary>
  /// Registers the coordinator services used by the Control API and remote Agent requests.
  /// </summary>
  /// <param name="services">The service collection to add coordinator services to.</param>
  public static void AddCoordinatorServices( this IServiceCollection services ) {
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
  }
}