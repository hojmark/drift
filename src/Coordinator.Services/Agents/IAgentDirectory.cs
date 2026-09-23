using Drift.Coordinator.Services.Models;
using Drift.Domain;

namespace Drift.Coordinator.Services.Agents;

/// <summary>
/// Provides access to enrolled agents and their transient connection state.
/// </summary>
public interface IAgentDirectory {
  IReadOnlyCollection<EnrolledAgent> GetEnrolledAgents();
  AgentConnectionStatus GetConnectionStatus( AgentId id );
  bool MarkConnected( AgentId id );
  bool MarkUnavailable( AgentId id );
  bool TryGet( AgentId id, out EnrolledAgent? agent );
  EnrolledAgent Enroll( AgentId id, Uri address );
  bool Remove( AgentId id );
}