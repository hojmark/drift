using Drift.Networking.Core;
using Drift.Networking.Core.Messages;
using Drift.Networking.Tests.Helpers;
using Drift.TestUtilities;

namespace Drift.Networking.Tests;

internal sealed class MessageStreamManagerTests {
  [Test]
  public async Task IncomingMessageIsDispatchedToHandler() {
    // Arrange
    var cts = new CancellationTokenSource();
    var logger = new StringLogger( TestContext.Out );
    var messageHandler = new TestMessageHandler();
    var streamManager = new MessageStreamManager(
      logger,
      null,
      new MessageDispatcher(
        [messageHandler],
        new MessageEnvelopeConverter(),
        new MessageResponseCorrelator( logger ),
        logger
      ),
      new MessagingOptions { StoppingToken = cts.Token }
    );

    var callContext = TestServerCallContext.Create();
    callContext.RequestHeaders.Add( "agent-id", "agentid_test123" );
    var duplexStreams = callContext.CreateDuplexStreams();
    var serverStreams = duplexStreams.Server;
    var stream = streamManager.Create( serverStreams.RequestStream, serverStreams.ResponseStream, callContext );
    var converter = new MessageEnvelopeConverter();

    // Act
    var clientStreams = duplexStreams.Client;
    await clientStreams.RequestStream.WriteAsync( converter.ToEnvelope<TestPeerMessage>( new TestPeerMessage {
      Payload = "test123"
    } ) );

    await cts.CancelAsync();
    await stream.ReadTask;

    // Assert
    Assert.That( messageHandler.LastMessage, Is.Not.Null );
    Assert.That( messageHandler.LastMessage.Payload, Is.EqualTo( "test123" ) );

    cts.Dispose();
  }
}