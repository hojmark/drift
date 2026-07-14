using Drift.Networking.Core;
using Drift.Networking.Core.Messages;
using Drift.Networking.Server;
using Drift.Networking.Tests.Helpers;
using Drift.TestUtilities;

namespace Drift.Networking.Tests;

internal sealed class InboundTests {
  [Test]
  public async Task InboundStreamIsClosedWhenCancelledTest() {
    // Arrange
    using var cts = new CancellationTokenSource();
    var inboundMessageService = CreateInboundMessageService( cts );

    var callContext = TestServerCallContext.Create();
    callContext.RequestHeaders.Add( "agent-id", "agentid_test123" );
    var duplexStreams = callContext.CreateDuplexStreams();

    // Act / Assert
    var serverStreams = duplexStreams.Server;
    var connectTask =
      inboundMessageService.Connect( serverStreams.RequestStream, serverStreams.ResponseStream, callContext );

    Assert.That( connectTask.IsCompleted, Is.False );

    await cts.CancelAsync();

    await Task.WhenAny( connectTask, Task.Delay( 1000 ) );

    Assert.That( connectTask.IsCompleted, Is.True );
  }


  [Test]
  public async Task InboundStreamRemainsOpenWhenNotCancelledTest() {
    // Arrange
    using var cts = new CancellationTokenSource();
    var inboundMessageService = CreateInboundMessageService( cts );

    var callContext = TestServerCallContext.Create();
    callContext.RequestHeaders.Add( "agent-id", "agentid_test123" );
    var duplexStreams = callContext.CreateDuplexStreams();

    // Act
    var connectTask = inboundMessageService.Connect(
      duplexStreams.Server.RequestStream,
      duplexStreams.Server.ResponseStream,
      callContext
    );

    // Assert
    Assert.That( connectTask.IsCompleted, Is.False );

    await Task.WhenAny( connectTask, Task.Delay( 1000 ) );

    Assert.That( connectTask.IsCompleted, Is.False );
  }

  private static InboundMessageService CreateInboundMessageService( CancellationTokenSource cts ) {
    var logger = new StringLogger( TestContext.Out );
    var messageStreamManager = new MessageStreamManager(
      logger,
      null,
      new MessageDispatcher( [], new MessageEnvelopeConverter(), new MessageResponseCorrelator( logger ), logger ),
      new MessagingOptions { StoppingToken = cts.Token }
    );
    return new InboundMessageService( messageStreamManager, logger );
  }
}