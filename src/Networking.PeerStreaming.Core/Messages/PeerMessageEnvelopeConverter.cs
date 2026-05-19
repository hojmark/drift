using System.Text.Json;
using Drift.Networking.Grpc.Generated;
using Drift.Networking.PeerStreaming.Core.Abstractions;

namespace Drift.Networking.PeerStreaming.Core.Messages;

internal sealed class PeerMessageEnvelopeConverter : IPeerMessageEnvelopeConverter {
  public PeerMessage ToEnvelope<T>( IPeerMessage message, string? requestId = null ) where T : IPeerMessage {
    string json = JsonSerializer.Serialize( message, T.JsonInfo );
    return new PeerMessage { MessageType = T.MessageType, Message = json, };
  }

  public T FromEnvelope<T>( PeerMessage envelope ) where T : IPeerMessage {
    if ( envelope.MessageType != T.MessageType ) {
      throw new InvalidOperationException(
        $"Envelope contains '{envelope.MessageType}' but caller expects '{T.MessageType}'."
      );
    }

    return JsonSerializer.Deserialize<T>( envelope.Message, T.JsonInfo.Options )!;
  }
}