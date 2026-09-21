using Drift.Coordinator.Services.Agents.Enrollment;
using Drift.Coordinator.Services.Models;
using Drift.Coordinator.Services.Spec;
using Drift.Domain;

namespace Drift.Coordinator.Services.Agents;

public sealed class AgentManagementService(
  IAgentDirectory agentDirectory,
  IAgentEnrollmentStore enrollmentStore,
  CoordinatorSpec coordinatorSpec
) {
  public IReadOnlyCollection<AgentState> ListAgents() {
    return agentDirectory.GetEnrolledAgents()
      .Select( ToState )
      .OrderBy( agent => agent.Id.Value )
      .ToArray();
  }

  public AgentState EnrollAgent( EnrollAgentCommand command ) {
    var declaredAgent = coordinatorSpec.View.Agents.SingleOrDefault( agent => agent.Id == command.Id.Value );
    if ( declaredAgent is null ) {
      throw new ArgumentException(
        $"Agent '{command.Id}' is not declared in the coordinator spec.",
        nameof(command)
      );
    }

    if ( !Uri.TryCreate( declaredAgent.Address, UriKind.Absolute, out var address ) ||
         address.Scheme is not ("http" or "https") ) {
      throw new ArgumentException(
        $"Agent '{command.Id}' has an invalid address in the coordinator spec.",
        nameof(command)
      );
    }

    var enrolledAgent = agentDirectory.Enroll( command.Id, address );
    var agentState = ToState( enrolledAgent );
    SaveEnrollments();
    return agentState;
  }

  public bool UnenrollAgent( AgentId id ) {
    var removed = agentDirectory.Remove( id );
    if ( removed ) {
      SaveEnrollments();
    }

    return removed;
  }

  private AgentState ToState( EnrolledAgent agent ) => new(
    agent.Id,
    agent.Address,
    AgentEnrollmentStatus.Enrolled,
    agentDirectory.GetConnectionStatus( agent.Id )
  );

  private void SaveEnrollments() {
    enrollmentStore.Save(
      agentDirectory.GetEnrolledAgents()
        .Select( agent => new AgentEnrollmentRecord( agent.Id, agent.Address.ToString(), agent.EnrolledAt ) )
        .ToArray()
    );
  }
}