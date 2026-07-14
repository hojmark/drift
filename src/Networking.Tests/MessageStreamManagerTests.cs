using Drift.Networking.Client;
using Drift.Networking.Core;
using Drift.Networking.Core.Abstractions;
using Drift.Networking.Core.Messages;
using Drift.Networking.Tests.Helpers;
using Drift.TestUtilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Drift.Networking.Tests;

internal sealed class MessageStreamManagerTests {
  [Test]
  public async Task IncomingMessageIsDispatchedToHandler() {
    // Arrange
    var cts = new CancellationTokenSource();
    var (streamManager, messageHandler) = CreateStreamManager( cts );

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

  private static (IMessageStreamManager, TestMessageHandler messageHandler) CreateStreamManager(
    CancellationTokenSource cts
  ) {
    var serviceCollection = new ServiceCollection();
    var logger = new StringLogger( TestContext.Out );
    var messageHandler = new TestMessageHandler( logger );
    serviceCollection.AddSingleton<ILogger>( logger );
    serviceCollection.AddSingleton<IMessageHandler>( _ => messageHandler );
    serviceCollection.AddMessagingCore( new MessagingOptions { StoppingToken = cts.Token } );
    serviceCollection.AddMessagingClient();
    var serviceProvider = serviceCollection.BuildServiceProvider();
    return ( serviceProvider.GetRequiredService<IMessageStreamManager>(), messageHandler );
  }
}