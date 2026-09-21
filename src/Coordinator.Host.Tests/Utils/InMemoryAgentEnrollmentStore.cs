using Drift.Coordinator.Services.Agents.Enrollment;

namespace Drift.Coordinator.Host.Tests.Utils;

internal sealed class InMemoryAgentEnrollmentStore : IAgentEnrollmentStore {
  public List<AgentEnrollmentRecord> Records {
    get;
    private set;
  } = [];

  public IReadOnlyCollection<AgentEnrollmentRecord> Load() => Records;

  public void Save( IReadOnlyCollection<AgentEnrollmentRecord> records ) {
    Records = records.ToList();
  }
}