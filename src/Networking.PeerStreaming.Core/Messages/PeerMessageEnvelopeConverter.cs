using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Drift.Networking.Grpc.Generated;
using Drift.Networking.PeerStreaming.Core.Abstractions;
using Drift.Serialization.Converters;

namespace Drift.Networking.PeerStreaming.Core.Messages;

internal sealed class PeerMessageEnvelopeConverter : IPeerMessageEnvelopeConverter {
  public PeerMessage ToEnvelope<T>( IPeerMessage message, string? requestId = null )
    where T : IPeerMessage {
    string json = JsonSerializer.Serialize( message, T.JsonInfo );
    return new PeerMessage { MessageType = T.MessageType, Message = json, };
  }

  public TSelf FromEnvelope<TSelf>( PeerMessage envelope ) where TSelf : IPeerMessage {
    if ( envelope.MessageType != TSelf.MessageType ) {
      throw new InvalidOperationException(
        $"Envelope contains '{envelope.MessageType}' but caller expects '{TSelf.MessageType}'."
      );
    }

    return JsonSerializer.Deserialize<TSelf>( envelope.Message, TSelf.JsonInfo.Options )!;
  }
}