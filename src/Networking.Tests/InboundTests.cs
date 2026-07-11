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
    var logger = new StringLogger( TestContext.Out );
    var responseCorrelator = new MessageResponseCorrelator( logger );
    var envelopeConverter = new MessageEnvelopeConverter();
    var messageStreamManager = new MessageStreamManager(
      logger,
      null,
      new MessageDispatcher( [], envelopeConverter, responseCorrelator, logger ),
      new MessagingOptions { StoppingToken = cts.Token }
    );

    var callContext = TestServerCallContext.Create();
    callContext.RequestHeaders.Add( "agent-id", "agentid_test123" );
    var duplexStreams = callContext.CreateDuplexStreams();

    var inboundMessageService = new InboundMessageService( messageStreamManager, logger );

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
    var logger = new StringLogger( TestContext.Out );
    var responseCorrelator = new MessageResponseCorrelator( logger );
    var envelopeConverter = new MessageEnvelopeConverter();
    var messageStreamManager = new MessageStreamManager(
      logger,
      null,
      new MessageDispatcher( [], envelopeConverter, responseCorrelator, logger ),
      new MessagingOptions { StoppingToken = cts.Token }
    );
    var inboundMessageService = new InboundMessageService( messageStreamManager, logger );

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
}