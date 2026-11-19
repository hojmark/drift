using Drift.Domain;

namespace Drift.Coordinator.Services.Agents;

/// <summary>
/// Represents an enrolled agent together with state learned from its latest connection attempt.
/// </summary>
public sealed class EnrolledAgent(
  AgentId id,
  Uri address,
  DateTimeOffset enrolledAt
) {
  public AgentId Id => id;

  public Uri Address => address;

  public DateTimeOffset EnrolledAt => enrolledAt;

  public Agent ToDomainAgent() => new() { Id = id.Value, Address = address.ToString(), Authentication = new() };
}