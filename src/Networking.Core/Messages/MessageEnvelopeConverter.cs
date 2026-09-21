using System.Text.Json;
using Drift.Domain;
using Drift.Networking.Core.Abstractions;
using Drift.Networking.Grpc.Generated;

namespace Drift.Networking.Core.Messages;

internal sealed class MessageEnvelopeConverter : IMessageEnvelopeConverter {
  public Message ToEnvelope<TRequest, TResponse>( TRequest message, RequestId requestId )
    where TRequest : IRequest<TResponse> where TResponse : IResponse {
    return new Message {
      MessageType = TRequest.MessageType,
      Payload = JsonSerializer.Serialize( message, TRequest.JsonInfo ),
      RequestId = requestId.ToString()
    };
  }

  public Message ToEnvelope<TResponse>( TResponse message, RequestId replyTo ) where TResponse : IResponse {
    return new Message {
      MessageType = TResponse.MessageType,
      Payload = JsonSerializer.Serialize( message, TResponse.JsonInfo ),
      ReplyTo = replyTo.ToString()
    };
  }

  public TRequest FromRequestEnvelope<TRequest, TResponse>( Message envelope )
    where TRequest : IRequest<TResponse> where TResponse : IResponse {
    ValidateRequestEnvelope<TRequest>( envelope );
    return JsonSerializer.Deserialize<TRequest>( envelope.Payload, TRequest.JsonInfo.Options )!;
  }

  public TResponse FromResponseEnvelope<TResponse>( Message envelope ) where TResponse : IResponse {
    ValidateResponseEnvelope<TResponse>( envelope );
    return JsonSerializer.Deserialize<TResponse>( envelope.Payload, TResponse.JsonInfo.Options )!;
  }

  private static void ValidateRequestEnvelope<TRequest>( Message envelope ) where TRequest : IMessage {
    if ( envelope.MessageType != TRequest.MessageType ) {
      throw new InvalidOperationException(
        $"Envelope contains '{envelope.MessageType}' but caller expects '{TRequest.MessageType}'."
      );
    }

    if ( string.IsNullOrWhiteSpace( envelope.RequestId ) ) {
      throw new InvalidOperationException(
        $"{nameof(Message.RequestId)} is required on a request envelope."
      );
    }

    if ( !string.IsNullOrWhiteSpace( envelope.ReplyTo ) ) {
      throw new InvalidOperationException(
        $"{nameof(Message.ReplyTo)} must be empty on a request envelope."
      );
    }

    _ = RequestId.Parse( envelope.RequestId );
  }

  private static void ValidateResponseEnvelope<TResponse>( Message envelope ) where TResponse : IMessage {
    if ( envelope.MessageType != TResponse.MessageType ) {
      throw new InvalidOperationException(
        $"Envelope contains '{envelope.MessageType}' but caller expects '{TResponse.MessageType}'."
      );
    }

    if ( !string.IsNullOrWhiteSpace( envelope.RequestId ) ) {
      throw new InvalidOperationException(
        $"{nameof(Message.RequestId)} must be empty on a response envelope."
      );
    }

    if ( string.IsNullOrWhiteSpace( envelope.ReplyTo ) ) {
      throw new InvalidOperationException(
        $"{nameof(Message.ReplyTo)} is required on a response envelope."
      );
    }

    _ = RequestId.Parse( envelope.ReplyTo );
  }
}
