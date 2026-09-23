using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Drift.Networking.Core.Abstractions;
using Microsoft.Extensions.Logging;

namespace Drift.Networking.Tests.Helpers;

internal sealed class TestMessage : IRequest<TestMessage>, IResponse {
  public static string MessageType => "test-message";

  public string Payload {
    get;
    init;
  } = string.Empty;

  public static JsonTypeInfo JsonInfo => TestPeerMessageJsonContext.Default.TestMessage;
}

[JsonSerializable( typeof(TestMessage) )]
internal sealed partial class TestPeerMessageJsonContext : JsonSerializerContext;

internal sealed class TestMessageHandler( ILogger logger ) : RequestHandler<TestMessage, TestMessage>( logger ) {
  public TestMessage? LastMessage {
    get;
    private set;
  }

  public override Task HandleAsync(
    TestMessage message,
    IMessageResponder<TestMessage> responder,
    CancellationToken cancellationToken
  ) {
    Logger.LogInformation( "Received message of type '{MessageType}'", MessageType );

    LastMessage = message;

    Logger.LogInformation( "Handled message with payload '{Payload}'", message.Payload );

    // For this test handler, we don't send a response
    return Task.CompletedTask;
  }
}