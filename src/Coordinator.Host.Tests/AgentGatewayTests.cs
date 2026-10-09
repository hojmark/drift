using Drift.Coordinator.Host.Tests.Utils;
using Drift.Coordinator.Services.Agents;
using Drift.Coordinator.Services.Models;
using Drift.Domain;
using Drift.Messaging.Client;
using Drift.Messaging.Protocol.Agent.Status;
using Drift.Networking.Core.Abstractions;
using Drift.TestUtilities.IO;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Drift.Coordinator.Host.Tests;

internal sealed class AgentGatewayTests {
  [Test]
  public async Task CheckStatusAsync_WhenAgentIsReady_MarksAgentConnected() {
    var id = AgentId.Parse( "agent_one", null );
    var directory = new InMemoryAgentDirectory( [CreateAgent( id )] );
    await using var streams = new FakeMessageStreamManager();
    using var gateway = new AgentGateway( new FakeAgentClient(), directory, streams, NullLogger.Instance );

    await gateway.CheckStatusAsync( id, CancellationToken.None );

    Assert.That( directory.GetConnectionStatus( id ), Is.EqualTo( AgentConnectionStatus.Connected ) );
  }

  [Test]
  public async Task CheckStatusAsync_WhenAgentTransitionsFromUnknownToConnected_LogsStateChangeOnce() {
    var id = AgentId.Parse( "agent_one", null );
    var directory = new InMemoryAgentDirectory( [CreateAgent( id )] );
    var logger = new TestLogger( captureEntries: true );
    await using var streams = new FakeMessageStreamManager();
    using var gateway = new AgentGateway( new FakeAgentClient(), directory, streams, logger );

    await gateway.CheckStatusAsync( id, CancellationToken.None );
    await gateway.CheckStatusAsync( id, CancellationToken.None );

    var stateChangeLogs = logger.Entries
      .Where( entry => entry.Message.Contains( "changed from Unknown to Connected", StringComparison.Ordinal ) )
      .ToArray();
    using ( Assert.EnterMultipleScope() ) {
      Assert.That( stateChangeLogs, Has.Length.EqualTo( 1 ) );
      Assert.That( stateChangeLogs[0].Level, Is.EqualTo( LogLevel.Information ) );
    }
  }

  [Test]
  public void CheckStatusAsync_WhenRequestFails_MarksAgentUnavailableAndRethrows() {
    var id = AgentId.Parse( "agent_one", null );
    var directory = new InMemoryAgentDirectory( [CreateAgent( id )] );
    var exception = new InvalidOperationException( "request failed" );
    using var streams = new FakeMessageStreamManager();
    using var gateway = new AgentGateway( new FakeAgentClient( exception ), directory, streams, NullLogger.Instance );

    var thrown = Assert.ThrowsAsync<InvalidOperationException>( async () =>
      await gateway.CheckStatusAsync( id, CancellationToken.None )
    );

    using ( Assert.EnterMultipleScope() ) {
      Assert.That( thrown, Is.SameAs( exception ) );
      Assert.That( directory.GetConnectionStatus( id ), Is.EqualTo( AgentConnectionStatus.Unavailable ) );
    }
  }

  [Test]
  public async Task CheckStatusAsync_LogsOnlyUnavailableAndRecoveryTransitions() {
    var id = AgentId.Parse( "agent_one", null );
    var directory = new InMemoryAgentDirectory( [CreateAgent( id )] );
    var client = new FakeAgentClient( new InvalidOperationException( "Connection refused" ) );
    var logger = new TestLogger( captureEntries: true );
    await using var streams = new FakeMessageStreamManager();
    using var gateway = new AgentGateway( client, directory, streams, logger );

    for ( var attempt = 0; attempt < 2; attempt++ ) {
      Assert.ThrowsAsync<InvalidOperationException>( async () =>
        await gateway.CheckStatusAsync( id, CancellationToken.None )
      );
    }

    client.Exception = null;
    await gateway.CheckStatusAsync( id, CancellationToken.None );

    var warningLogs = logger.Entries.Where( entry => entry.Level == LogLevel.Warning ).ToArray();
    var informationLogs = logger.Entries.Where( entry => entry.Level == LogLevel.Information ).ToArray();
    using ( Assert.EnterMultipleScope() ) {
      Assert.That( warningLogs, Has.Length.EqualTo( 1 ) );
      Assert.That( warningLogs[0].Message, Does.Contain( "Connection to agent agent_one" ) );
      Assert.That( warningLogs[0].Message, Does.Contain( "Connection refused" ) );
      Assert.That( informationLogs, Has.Length.EqualTo( 1 ) );
      Assert.That( informationLogs[0].Message, Does.Contain( "changed from Unavailable to Connected" ) );
    }
  }

  [Test]
  public async Task ConnectionClosureOnlyMarksAgentUnavailableWhenClosureIsRemote() {
    var id = AgentId.Parse( "agent_one", null );
    var directory = new InMemoryAgentDirectory( [CreateAgent( id )] );
    var logger = new TestLogger( captureEntries: true );
    await using var streams = new FakeMessageStreamManager();
    using var gateway = new AgentGateway( new FakeAgentClient(), directory, streams, logger );
    await gateway.CheckStatusAsync( id, CancellationToken.None );

    streams.Close( id, ConnectionCloseOrigin.Local );
    Assert.That( directory.GetConnectionStatus( id ), Is.EqualTo( AgentConnectionStatus.Connected ) );
    streams.Close( id, ConnectionCloseOrigin.Unknown );

    using ( Assert.EnterMultipleScope() ) {
      Assert.That( directory.GetConnectionStatus( id ), Is.EqualTo( AgentConnectionStatus.Unavailable ) );
      Assert.That(
        logger.Entries.Count( entry => entry.Level == LogLevel.Warning &&
                                       entry.Message.Contains(
                                         "changed from Connected to Unavailable",
                                         StringComparison.Ordinal
                                       )
        ),
        Is.EqualTo( 1 )
      );
    }
  }

  private static EnrolledAgent CreateAgent( AgentId id ) {
    return new EnrolledAgent( id, new Uri( "http://127.0.0.1:5001" ), DateTimeOffset.UtcNow );
  }

  private sealed class FakeAgentClient( Exception? exception = null ) : IAgentClient {
    public Exception? Exception {
      get;
      set;
    } = exception;

    public Task<TResponse> RequestAsync<TRequest, TResponse>(
      Domain.Agent agent,
      TRequest message,
      TimeSpan? timeout = null,
      CancellationToken cancellationToken = default
    ) where TResponse : IResponse where TRequest : IRequest<TResponse> {
      if ( Exception is not null ) {
        return Task.FromException<TResponse>( Exception );
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