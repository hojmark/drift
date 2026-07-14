using Drift.Networking.Client;
using Drift.Networking.Core;
using Drift.Networking.Core.Abstractions;
using Drift.Networking.Server;
using Drift.Networking.Tests.Helpers;
using Drift.TestUtilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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
    var serviceCollection = new ServiceCollection();
    var logger = new StringLogger( TestContext.Out );
    serviceCollection.AddSingleton<ILogger>( logger );
    serviceCollection.AddMessagingCore( new MessagingOptions { StoppingToken = cts.Token } );
    serviceCollection.AddMessagingClient();
    serviceCollection.AddMessagingServer();
    var serviceProvider = serviceCollection.BuildServiceProvider();
    var streamManager = serviceProvider.GetRequiredService<IMessageStreamManager>();
    return new InboundMessageService( streamManager, logger );
  }
}