using Drift.Networking.PeerStreaming.Core;
using Drift.Networking.PeerStreaming.Core.Messages;
using Drift.Networking.PeerStreaming.Tests.Helpers;
using Drift.TestUtilities;

namespace Drift.Networking.PeerStreaming.Tests;

internal sealed class PeerStreamManagerTests {
  [Test]
  public async Task IncomingMessageIsDispatchedToHandler() {
    // Arrange
    var cts = new CancellationTokenSource();
    var logger = new StringLogger( TestContext.Out );
    var testMessageHandler = new TestMessageHandler();
    var envelopeConverter = new PeerMessageEnvelopeConverter();
    var dispatcher = new PeerMessageDispatcher( [testMessageHandler], envelopeConverter, null, logger );
    var peerStreamManager = new PeerStreamManager(
      logger,
      null,
      dispatcher,
      new PeerStreamingOptions { StoppingToken = cts.Token }
    );

    var callContext = TestServerCallContext.Create();
    callContext.RequestHeaders.Add( "agent-id", "agentid_test123" );
    var duplexStreams = callContext.CreateDuplexStreams();
    var serverStreams = duplexStreams.Server;
    var stream = peerStreamManager.Create( serverStreams.RequestStream, serverStreams.ResponseStream, callContext );
    var converter = new PeerMessageEnvelopeConverter();

    // Act
    var clientStreams = duplexStreams.Client;
    await clientStreams.RequestStream.WriteAsync( converter.ToEnvelope<TestPeerMessage>( new TestPeerMessage() ) );

    await cts.CancelAsync();
    await stream.ReadTask;

    // Assert
    Assert.That( testMessageHandler.LastMessage, Is.Not.Null );
    //Assert.That( testMessageHandler.LastMessage.MessageType, Is.EqualTo( "TestMessageType" ) );

    cts.Dispose();
  }
}