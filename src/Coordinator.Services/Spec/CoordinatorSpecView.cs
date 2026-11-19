using Drift.Domain;

namespace Drift.Coordinator.Services.Spec;

/// <summary>
/// Read-only view of the coordinator's complete declarative inventory.
/// </summary>
public sealed record CoordinatorSpecView(
  Network Network,
  Server? Server,
  Settings? Settings,
  IReadOnlyList<Agent> Agents
);
