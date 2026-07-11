using System.Text.Json;
using Drift.Networking.Core.Abstractions;
using Drift.Networking.Grpc.Generated;

namespace Drift.Networking.Core.Messages;

internal sealed class MessageEnvelopeConverter : IMessageEnvelopeConverter {
  public Message ToEnvelope<T>( IMessage message, string? requestId = null ) where T : IMessage {
    string json = JsonSerializer.Serialize( message, T.JsonInfo );
    return new Message { MessageType = T.MessageType, Payload = json, };
  }

  public T FromEnvelope<T>( Message envelope ) where T : IMessage {
    if ( envelope.MessageType != T.MessageType ) {
      throw new InvalidOperationException(
        $"Envelope contains '{envelope.MessageType}' but caller expects '{T.MessageType}'."
      );
    }

    return JsonSerializer.Deserialize<T>( envelope.Payload, T.JsonInfo.Options )!;
  }
}