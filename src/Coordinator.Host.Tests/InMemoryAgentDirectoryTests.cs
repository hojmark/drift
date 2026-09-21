using Drift.Coordinator.Services.Agents;
using Drift.Coordinator.Services.Models;
using Drift.Domain;

namespace Drift.Coordinator.Host.Tests;

internal sealed class InMemoryAgentDirectoryTests {
  [Test]
  public void NewRegistry_StartsAgentAsUnknown() {
    var id = AgentId.Parse( "agent_one", null );
    var address = new Uri( "http://127.0.0.1:5001" );
    var agent = new EnrolledAgent( id, address, DateTimeOffset.UtcNow );

    var directory = new InMemoryAgentDirectory( [agent] );
    directory.MarkConnected( id );

    var restartedDirectory = new InMemoryAgentDirectory( [new EnrolledAgent( id, address, agent.EnrolledAt )] );
    var enrolledAgent = restartedDirectory.GetEnrolledAgents().Single();

    using ( Assert.EnterMultipleScope() ) {
      Assert.That( enrolledAgent.Id.Value, Is.EqualTo( id.Value ) );
      Assert.That( enrolledAgent.Address, Is.EqualTo( address ) );
      Assert.That( restartedDirectory.GetConnectionStatus( id ), Is.EqualTo( AgentConnectionStatus.Unknown ) );
    }
  }

  [Test]
  public void EquivalentAgentIds_ReferToTheSameRegistration() {
    var directory = new InMemoryAgentDirectory(
      Array.Empty<EnrolledAgent>()
    );
    var enrolledId = AgentId.Parse( "agent_one", null );
    var equivalentId = AgentId.Parse( "agent_one", null );

    directory.Enroll( enrolledId, new Uri( "http://127.0.0.1:5001" ) );

    Assert.That( directory.TryGet( equivalentId, out _ ), Is.True );
  }
}