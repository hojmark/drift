using Drift.Coordinator.Client.Models;

namespace Drift.Cli.Commands.Status;

internal sealed record StatusEnvironment(
  string Name,
  string Address,
  bool IsActive,
  bool IsLocal,
  string State,
  IReadOnlyCollection<CoordinatorAgentStatus> Agents,
  string? Error
);