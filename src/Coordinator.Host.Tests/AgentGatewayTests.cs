using Drift.Coordinator.Services.Agents;
using Drift.Coordinator.Services.Models;
using Drift.Domain;
using Drift.Messaging.Client;
using Drift.Messaging.Protocol.Agent.Status;
using Drift.Networking.Core.Abstractions;

namespace Drift.Coordinator.Host.Tests;

internal sealed class AgentGatewayTests {
  [Test]
  public async Task CheckStatusAsync_WhenAgentIsReady_MarksAgentConnected() {
    var id = AgentId.Parse( "agent_one", null );
    var directory = new InMemoryAgentDirectory( [CreateAgent( id )] );
    var gateway = new AgentGateway( new FakeAgentClient(), directory );

    await gateway.CheckStatusAsync( id, CancellationToken.None );

    Assert.That( directory.GetConnectionStatus( id ), Is.EqualTo( AgentConnectionStatus.Connected ) );
  }

  [Test]
  public void CheckStatusAsync_WhenRequestFails_MarksAgentUnavailableAndRethrows() {
    var id = AgentId.Parse( "agent_one", null );
    var directory = new InMemoryAgentDirectory( [CreateAgent( id )] );
    var exception = new InvalidOperationException( "request failed" );
    var gateway = new AgentGateway( new FakeAgentClient( exception ), directory );

    var thrown = Assert.ThrowsAsync<InvalidOperationException>( async () =>
      await gateway.CheckStatusAsync( id, CancellationToken.None )
    );

    using ( Assert.EnterMultipleScope() ) {
      Assert.That( thrown, Is.SameAs( exception ) );
      Assert.That( directory.GetConnectionStatus( id ), Is.EqualTo( AgentConnectionStatus.Unavailable ) );
    }
  }

  private static EnrolledAgent CreateAgent( AgentId id ) {
    return new EnrolledAgent( id, new Uri( "http://127.0.0.1:5001" ), DateTimeOffset.UtcNow );
  }

  private sealed class FakeAgentClient( Exception? exception = null ) : IAgentClient {
    public Task<TResponse> RequestAsync<TRequest, TResponse>(
      Domain.Agent agent,
      TRequest message,
      TimeSpan? timeout = null,
      CancellationToken cancellationToken = default
    ) where TResponse : IResponse where TRequest : IRequest<TResponse> {
      if ( exception is not null ) {
        return Task.FromException<TResponse>( exception );
      }

      return Task.FromResult( (TResponse) (IResponse) new AgentStatusResponse { Status = AgentStatus.Ready } );
    }

    public Task<TResponse> RequestStreamingAsync<TRequest, TProgress, TResponse>(
      Domain.Agent agent,
      TRequest message,
      Action<TProgress> onProgress,
      TimeSpan? timeout = null,
      CancellationToken cancellationToken = default
    ) where TRequest : IStreamingRequest<TProgress, TResponse>
      where TProgress : IResponse
      where TResponse : IResponse {
      throw new NotSupportedException();
    }
  }
}