using Drift.Domain;
using Drift.Networking.Client;
using Drift.Networking.Core;
using Drift.Networking.Core.Abstractions;
using Drift.Networking.Core.Messages;
using Drift.Networking.Tests.Helpers;
using Drift.TestUtilities.IO;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Drift.Networking.Tests;

internal sealed class MessageStreamManagerTests {
  [Test]
  public async Task OutboundStreamAttemptLogsACreatedAndDisposedPair() {
    var logger = new StringLogger( minimumLogLevel: LogLevel.Debug );
    await using var fixture = new MessageStreamManagerTestFixture( logger );

    var connection = fixture.Manager.GetOrCreate(
      new Uri( "http://127.0.0.1:1" ),
      AgentId.Parse( "agent_unavailable", null )
    );
    await connection.Completion.WaitAsync( TimeSpan.FromSeconds( 5 ) ); // Should fail almost immediately
    await connection.DisposeAsync();

    using ( Assert.EnterMultipleScope() ) {
      Assert.That( logger.ToString(), Does.Contain( "Outbound stream #" ).And.Contain( "created" ) );
      Assert.That( logger.ToString(), Does.Contain( "Outbound stream #" ).And.Contain( "disposed" ) );
    }
  }

  [Test]
  public async Task IncomingMessageIsDispatchedToHandler() {
    await using var fixture = new MessageStreamManagerTestFixture();
    var connection = fixture.OpenInbound( "agent_test123" );
    var converter = new MessageEnvelopeConverter();

    await connection.RequestStream.WriteAsync(
      converter.ToEnvelope<TestMessage, TestMessage>( new TestMessage { Payload = "test123" }, RequestId.New() )
    );
    await fixture.StopAsync();
    await connection.Completion;

    Assert.That( fixture.MessageHandler.LastMessage?.Payload, Is.EqualTo( "test123" ) );
  }

  [Test]
  public async Task StreamIsSharedAcrossScopes() {
    await using var fixture = new MessageStreamManagerTestFixture();
    await using var first = fixture.Services.CreateAsyncScope();
    await using var second = fixture.Services.CreateAsyncScope();
    var firstManager = first.ServiceProvider.GetRequiredService<IMessageStreamManager>();
    var secondManager = second.ServiceProvider.GetRequiredService<IMessageStreamManager>();
    var firstConnection = fixture.OpenInbound( "agent_test123", firstManager );
    var secondConnection = secondManager.GetOrCreate(
      new Uri( "http://127.0.0.1:5001" ),
      AgentId.Parse( "agent_test123", null )
    );

    Assert.That( secondConnection.Stream, Is.SameAs( firstConnection.Stream ) );
    using ( Assert.EnterMultipleScope() ) {
      Assert.That( firstConnection.Stream.RemoteId, Is.EqualTo( AgentId.Parse( "agent_test123", null ) ) );
      Assert.That( firstConnection.Stream.Side, Is.EqualTo( ConnectionSide.Inbound ) );
      Assert.That( firstConnection.Completion.IsCompleted, Is.False );
    }
  }
}