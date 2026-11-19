using Drift.Coordinator.Services.Models;
using Drift.Coordinator.Services.State;

namespace Drift.Coordinator.Services;

/// <summary>
/// Provides the coordinator status exposed through the Control API.
/// </summary>
public static class ServerStatusService {
  public static ServerStatusResult GetStatus( ICoordinatorSpecStore specStore ) => new(
    string.IsNullOrWhiteSpace( specStore.LoadYaml() ) ? ServerStatus.SpecMissing : ServerStatus.Ready
  );
}