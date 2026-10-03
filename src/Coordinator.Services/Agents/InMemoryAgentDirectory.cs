using System.Collections.Concurrent;
using Drift.Coordinator.Services.Models;
using Drift.Domain;

namespace Drift.Coordinator.Services.Agents;

internal sealed class InMemoryAgentDirectory : IAgentDirectory {
  private readonly ConcurrentDictionary<AgentId, EnrolledAgent> _agents = new();
  private readonly ConcurrentDictionary<AgentId, AgentConnectionStatus> _connectionStatuses = new();

  public InMemoryAgentDirectory( IEnumerable<EnrolledAgent> initialAgents ) {
    foreach ( var agent in initialAgents ) {
      _agents[agent.Id] = agent;
      _connectionStatuses[agent.Id] = AgentConnectionStatus.Unknown;
    }
  }

  /// <summary>
  /// Adds or updates an enrolled agent in the in-memory directory.
  /// </summary>
  public EnrolledAgent Enroll( AgentId id, Uri address ) {
    var enrolledAgent = new EnrolledAgent( id, address, DateTimeOffset.UtcNow );
    _agents[id] = enrolledAgent;
    _connectionStatuses[id] = AgentConnectionStatus.Unknown;
    return enrolledAgent;
  }

  /// <summary>
  /// Removes an agent from the in-memory directory.
  /// </summary>
  public bool Remove( AgentId id ) {
    var removed = _agents.TryRemove( id, out _ );
    _connectionStatuses.TryRemove( id, out _ );
    return removed;
  }

  public bool TryGet( AgentId id, out EnrolledAgent? agent ) => _agents.TryGetValue( id, out agent );

  public IReadOnlyCollection<EnrolledAgent> GetEnrolledAgents() => _agents.Values
    .OrderBy( agent => agent.Id.Value )
    .ToArray();

  public AgentConnectionStatus GetConnectionStatus( AgentId id ) {
    return _connectionStatuses.GetValueOrDefault( id, AgentConnectionStatus.Unknown );
  }

  public AgentConnectionStatus? MarkConnected( AgentId id ) {
    return SetConnectionStatus( id, AgentConnectionStatus.Connected );
  }

  public AgentConnectionStatus? MarkUnavailable( AgentId id ) {
    return SetConnectionStatus( id, AgentConnectionStatus.Unavailable );
  }

  private AgentConnectionStatus? SetConnectionStatus( AgentId id, AgentConnectionStatus status ) {
    // Retry if another caller changes the status before the update.
    while ( _connectionStatuses.TryGetValue( id, out var previousStatus ) ) {
      if ( previousStatus == status || _connectionStatuses.TryUpdate( id, status, previousStatus ) ) {
        return previousStatus;
      }
    }

    return null;
  }
}