using Drift.Networking.PeerStreaming.Core;
using Drift.Networking.PeerStreaming.Core.Messages;
using Drift.Networking.PeerStreaming.Server;
using Drift.Networking.PeerStreaming.Tests.Helpers;
using Drift.TestUtilities;

namespace Drift.Networking.PeerStreaming.Tests;

internal sealed class InboundTests {
  [Test]
  public async Task InboundStreamIsClosedWhenCancelledTest() {
    // Arrange
    using var cts = new CancellationTokenSource();
    var logger = new StringLogger( TestContext.Out );
    var peerStreamManager = new PeerStreamManager(
      logger,
      null,
      new PeerMessageDispatcher( [], null, null, logger ),
      new PeerStreamingOptions { StoppingToken = cts.Token }
    );

    var callContext = TestServerCallContext.Create();
    callContext.RequestHeaders.Add( "agent-id", "agentid_test123" );
    var duplexStreams = callContext.CreateDuplexStreams();

    var inboundPeerService = new InboundPeerService( peerStreamManager, logger );

    // Act / Assert
    var serverStreams = duplexStreams.Server;
    var peerStreamTask =
      inboundPeerService.PeerStream( serverStreams.RequestStream, serverStreams.ResponseStream, callContext );

    Assert.That( peerStreamTask.IsCompleted, Is.False );

    await cts.CancelAsync();

    await Task.WhenAny( peerStreamTask, Task.Delay( 1000 ) );

    Assert.That( peerStreamTask.IsCompleted, Is.True );
  }

  [Test]
  public async Task InboundStreamRemainsOpenWhenNotCancelledTest() {
    // Arrange
    using var cts = new CancellationTokenSource();
    var logger = new StringLogger( TestContext.Out );
    var peerStreamManager = new PeerStreamManager(
      logger,
      null,
      new PeerMessageDispatcher( [], null, null, logger ),
      new PeerStreamingOptions { StoppingToken = cts.Token }
    );
    var inboundPeerService = new InboundPeerService( peerStreamManager, logger );

    var callContext = TestServerCallContext.Create();
    callContext.RequestHeaders.Add( "agent-id", "agentid_test123" );
    var duplexStreams = callContext.CreateDuplexStreams();

    // Act
    var peerStreamTask = inboundPeerService.PeerStream(
      duplexStreams.Server.RequestStream,
      duplexStreams.Server.ResponseStream,
      callContext
    );

    // Assert
    Assert.That( peerStreamTask.IsCompleted, Is.False );

    await Task.WhenAny( peerStreamTask, Task.Delay( 1000 ) );

    Assert.That( peerStreamTask.IsCompleted, Is.False );
  }
}