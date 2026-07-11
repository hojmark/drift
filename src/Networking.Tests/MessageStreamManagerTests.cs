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
    var testMessageHandler = new TestMessageHandler();
    var envelopeConverter = new MessageEnvelopeConverter();
    var responseCorrelator = new MessageResponseCorrelator( logger );
    var dispatcher = new MessageDispatcher( [testMessageHandler], envelopeConverter, responseCorrelator, logger );
    var messageStreamManager = new MessageStreamManager(
      logger,
      null,
      dispatcher,
      new MessagingOptions { StoppingToken = cts.Token }
    );

    var callContext = TestServerCallContext.Create();
    callContext.RequestHeaders.Add( "agent-id", "agentid_test123" );
    var duplexStreams = callContext.CreateDuplexStreams();
    var serverStreams = duplexStreams.Server;
    var stream = messageStreamManager.Create( serverStreams.RequestStream, serverStreams.ResponseStream, callContext );
    var converter = new MessageEnvelopeConverter();

    // Act
    var clientStreams = duplexStreams.Client;
    await clientStreams.RequestStream.WriteAsync( converter.ToEnvelope<TestPeerMessage>( new TestPeerMessage() ) );

    await cts.CancelAsync();
    await stream.ReadTask;

    // Assert
    Assert.That( testMessageHandler.LastMessage, Is.Not.Null );
    // Assert.That( testMessageHandler.LastMessage.MessageType, Is.EqualTo( "TestMessageType" ) );

    cts.Dispose();
  }
}