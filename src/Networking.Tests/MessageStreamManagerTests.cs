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
  public async Task IncomingMessageIsDispatchedToHandler() {
    // Arrange
    var cts = new CancellationTokenSource();
    var (streamManager, messageHandler) = CreateStreamManager( cts );

    var callContext = TestServerCallContext.Create();
    callContext.RequestHeaders.Add( "agent-id", "agent_test123" );
    var duplexStreams = callContext.CreateDuplexStreams();
    var serverStreams = duplexStreams.Server;
    var stream = streamManager.Create( serverStreams.RequestStream, serverStreams.ResponseStream, callContext );
    var converter = new MessageEnvelopeConverter();

    // Act
    var clientStreams = duplexStreams.Client;
    await clientStreams.RequestStream.WriteAsync(
      converter.ToEnvelope<TestMessage, TestMessage>(
        new TestMessage { Payload = "test123" },
        RequestId.New()
      )
    );

    await cts.CancelAsync();
    await stream.Stream.ReadTask;

    // Assert
    Assert.That( messageHandler.LastMessage, Is.Not.Null );
    Assert.That( messageHandler.LastMessage.Payload, Is.EqualTo( "test123" ) );

    cts.Dispose();
  }

  [Test]
  public async Task StreamIsSharedAcrossScopes() {
    using var cts = new CancellationTokenSource();
    var serviceCollection = new ServiceCollection();
    var logger = new StringLogger( TestContext.Out );
    serviceCollection.AddSingleton<ILogger>( logger );
    serviceCollection.AddSingleton<IMessageHandler>( _ => new TestMessageHandler( logger ) );
    serviceCollection.AddMessagingCore( new MessagingOptions { StoppingToken = cts.Token } );
    serviceCollection.AddMessagingClient();
    await using var serviceProvider = serviceCollection.BuildServiceProvider();

    var first = serviceProvider.CreateAsyncScope();
    var second = serviceProvider.CreateAsyncScope();
    try {
      var firstManager = first.ServiceProvider.GetRequiredService<IMessageStreamManager>();
      var secondManager = second.ServiceProvider.GetRequiredService<IMessageStreamManager>();
      var callContext = TestServerCallContext.Create();
      callContext.RequestHeaders.Add( "agent-id", "agent_test123" );
      var duplexStreams = callContext.CreateDuplexStreams();
      var firstConnection = firstManager.Create(
        duplexStreams.Server.RequestStream,
        duplexStreams.Server.ResponseStream,
        callContext
      );
      var secondConnection = secondManager.GetOrCreate(
        new Uri( "http://127.0.0.1:5001" ),
        AgentId.Parse( "agent_test123", null )
      );

      Assert.That( secondConnection.Stream, Is.SameAs( firstConnection.Stream ) );
      using ( Assert.EnterMultipleScope() ) {
        Assert.That( firstConnection.RemoteId, Is.EqualTo( AgentId.Parse( "agent_test123", null ) ) );
        Assert.That( firstConnection.Side, Is.EqualTo( ConnectionSide.Inbound ) );
        Assert.That( firstConnection.Completion.IsCompleted, Is.False );
      }
    }
    finally {
      await first.DisposeAsync();
      await second.DisposeAsync();
      await cts.CancelAsync();
    }
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
