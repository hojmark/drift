using Drift.Coordinator.Host.Tests.Utils;
using Drift.Coordinator.Services.Agents;
using Drift.Coordinator.Services.Models;
using Drift.Domain;

namespace Drift.Coordinator.Host.Tests;

internal sealed class AgentManagementServiceTests {
  [Test]
  public void EnrollAgent_AddsAgentToRegistry() {
    var service = CreateService();

    var state = service.EnrollAgent(
      new EnrollAgentCommand( AgentId.Parse( "agent_one", null ) )
    );

    using ( Assert.EnterMultipleScope() ) {
      Assert.That( state.Id.Value, Is.EqualTo( "agent_one" ) );
      Assert.That( state.Address, Is.EqualTo( new Uri( "http://127.0.0.1:5001" ) ) );
      Assert.That( state.EnrollmentStatus, Is.EqualTo( AgentEnrollmentStatus.Enrolled ) );
      Assert.That( state.ConnectionStatus, Is.EqualTo( AgentConnectionStatus.Unknown ) );
      Assert.That( service.ListAgents(), Has.One.Matches<AgentState>( a => a.Id.Value == "agent_one" ) );
    }
  }

  [Test]
  public void UnenrollAgent_RemovesEnrolledAgent() {
    var service = CreateService();
    service.EnrollAgent(
      new EnrollAgentCommand( AgentId.Parse( "agent_one", null ) )
    );

    var removed = service.UnenrollAgent( AgentId.Parse( "agent_one", null ) );

    using ( Assert.EnterMultipleScope() ) {
      Assert.That( removed, Is.True );
      Assert.That( service.ListAgents(), Is.Empty );
    }
  }

  [Test]
  public void EnrollAgent_RejectsAgentNotDeclaredInSpec() {
    var store = new InMemoryAgentEnrollmentStore();
    var service = new AgentManagementService(
      new InMemoryAgentDirectory( [] ),
      store,
      TestCoordinatorSpec.Create( false )
    );

    Assert.Throws<ArgumentException>( () => service.EnrollAgent(
        new EnrollAgentCommand( AgentId.Parse( "agent_one", null ) )
      )
    );
  }

  private static AgentManagementService CreateService() {
    return new AgentManagementService(
      new InMemoryAgentDirectory( [] ),
      new InMemoryAgentEnrollmentStore(),
      TestCoordinatorSpec.Create( true )
    );
  }
}