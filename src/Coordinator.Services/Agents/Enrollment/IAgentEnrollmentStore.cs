namespace Drift.Coordinator.Services.Agents.Enrollment;

/// <summary>
/// Persists enrolled agents.
/// </summary>
public interface IAgentEnrollmentStore {
  IReadOnlyCollection<AgentEnrollmentRecord> Load();
  void Save( IReadOnlyCollection<AgentEnrollmentRecord> records );
}