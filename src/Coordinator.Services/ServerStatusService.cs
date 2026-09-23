using Drift.Coordinator.Services.Models;

namespace Drift.Coordinator.Services;

/// <summary>
/// Provides the coordinator status exposed through the Control API.
/// </summary>
public static class ServerStatusService {
  public static ServerStatusResult GetStatus() => new( ServerStatus.Ready );
}