using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Drift.Networking.PeerStreaming.Core.Abstractions;

namespace Drift.Networking.PeerStreaming.Tests.Helpers;

internal sealed class TestPeerMessage : IPeerMessage {
  public static string MessageType => "testpeermessage";

  public static JsonTypeInfo JsonInfo => TestPeerMessageJsonContext.Default.TestPeerMessage;
}

[JsonSerializable( typeof(TestPeerMessage) )]
internal sealed partial class TestPeerMessageJsonContext : JsonSerializerContext;

internal sealed class TestMessageHandler : IPeerMessageHandler<TestPeerMessage, TestPeerMessage> {
  public TestPeerMessage? LastMessage {
    get;
    private set;
  }

  public string MessageType => TestPeerMessage.MessageType;

  public Task<TestPeerMessage?> HandleAsync( TestPeerMessage message, CancellationToken cancellationToken = default ) {
    LastMessage = message;
    return Task.FromResult<TestPeerMessage?>( null );
  }
}