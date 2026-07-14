using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Drift.Networking.Core.Abstractions;
using Drift.Networking.Grpc.Generated;

namespace Drift.Networking.Tests.Helpers;

internal sealed class TestPeerMessage : IRequest<TestPeerMessage>, IResponse {
  public static string MessageType => "test-peer-message";

  public string Payload {
    get;
    init;
  } = string.Empty;

  public static JsonTypeInfo JsonInfo => TestPeerMessageJsonContext.Default.TestPeerMessage;
}

[JsonSerializable( typeof(TestPeerMessage) )]
internal sealed partial class TestPeerMessageJsonContext : JsonSerializerContext;

internal sealed class TestMessageHandler : IMessageHandler {
  public TestPeerMessage? LastMessage {
    get;
    private set;
  }

  public string MessageType => TestPeerMessage.MessageType;

  public Task HandleAsync(
    Message envelope,
    IMessageEnvelopeConverter converter,
    IMessageStream stream,
    CancellationToken cancellationToken
  ) {
    var message = converter.FromEnvelope<TestPeerMessage>( envelope );
    LastMessage = message;

    // For this test handler, we don't send a response
    return Task.CompletedTask;
  }
}