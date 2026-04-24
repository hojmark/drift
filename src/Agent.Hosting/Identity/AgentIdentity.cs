using Drift.Domain;

namespace Drift.Agent.Hosting.Identity;

public partial class AgentIdentity {
  public required AgentId Id { get; init; }
  public required DateTime CreatedAt { get; init; }
  public string? ClusterId { get; init; }
  public DateTime? EnrolledAt { get; init; }
}
