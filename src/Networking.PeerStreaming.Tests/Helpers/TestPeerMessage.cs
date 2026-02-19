using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Drift.Networking.Grpc.Generated;
using Drift.Networking.PeerStreaming.Core.Abstractions;

namespace Drift.Networking.PeerStreaming.Tests.Helpers;

internal sealed class TestPeerMessage : IPeerRequest<TestPeerMessage>, IPeerResponse {
  public static string MessageType => "test-peer-message";

  public static JsonTypeInfo JsonInfo => TestPeerMessageJsonContext.Default.TestPeerMessage;
}

[JsonSerializable( typeof(TestPeerMessage) )]
internal sealed partial class TestPeerMessageJsonContext : JsonSerializerContext;

internal sealed class TestMessageHandler : IPeerMessageHandler {
  public TestPeerMessage? LastMessage {
    get;
    private set;
  }

  public string MessageType => TestPeerMessage.MessageType;

  public Task HandleAsync(
    PeerMessage envelope,
    IPeerMessageEnvelopeConverter converter,
    IPeerStream stream,
    CancellationToken cancellationToken
  ) {
    var message = converter.FromEnvelope<TestPeerMessage>( envelope );
    LastMessage = message;

    // For this test handler, we don't send a response
    return Task.CompletedTask;
  }
}