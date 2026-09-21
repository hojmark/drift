using Drift.Networking.Client;
using Drift.Networking.Core;
using Drift.Networking.Core.Abstractions;
using Drift.Networking.Server;
using Drift.Networking.Tests.Helpers;
using Drift.TestUtilities.IO;
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
    callContext.RequestHeaders.Add( "agent-id", "agent_test123" );
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
  public async Task InboundStreamCloseIsLogged() {
    using var cts = new CancellationTokenSource();
    var logger = new StringLogger();
    var inboundMessageService = CreateInboundMessageService( cts, logger );

    var callContext = TestServerCallContext.Create();
    callContext.RequestHeaders.Add( "agent-id", "agent_test123" );
    var duplexStreams = callContext.CreateDuplexStreams();
    var connectTask = inboundMessageService.Connect(
      duplexStreams.Server.RequestStream,
      duplexStreams.Server.ResponseStream,
      callContext
    );

    await cts.CancelAsync();
    await connectTask.WaitAsync( TimeSpan.FromSeconds( 1 ) );

    Assert.That( logger.ToString(), Does.Contain( "Inbound stream #" ) );
    Assert.That( logger.ToString(), Does.Contain( "agent_test123 closed" ) );
    Assert.That( logger.ToString(), Does.Not.Contain( "closed locally" ) );
  }

  [Test]
  public async Task InboundStreamRemainsOpenWhenNotCancelledTest() {
    // Arrange
    using var cts = new CancellationTokenSource();
    var inboundMessageService = CreateInboundMessageService( cts );

    var callContext = TestServerCallContext.Create();
    callContext.RequestHeaders.Add( "agent-id", "agent_test123" );
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
    return CreateInboundMessageService( cts, new StringLogger( TestContext.Out ) );
  }

  private static InboundMessageService CreateInboundMessageService(
    CancellationTokenSource cts,
    StringLogger logger
  ) {
    var serviceCollection = new ServiceCollection();
    serviceCollection.AddSingleton<ILogger>( logger );
    serviceCollection.AddMessagingCore( new MessagingOptions { StoppingToken = cts.Token } );
    serviceCollection.AddMessagingClient();
    serviceCollection.AddMessagingServer();
    var serviceProvider = serviceCollection.BuildServiceProvider();
    var streamManager = serviceProvider.GetRequiredService<IMessageStreamManager>();
    return new InboundMessageService( streamManager, logger );
  }
}
