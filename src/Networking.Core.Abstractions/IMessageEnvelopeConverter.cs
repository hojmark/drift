using Drift.Domain;
using Drift.Networking.Grpc.Generated;

namespace Drift.Networking.Core.Abstractions;

public interface IMessageEnvelopeConverter {
  /// <summary>
  /// Creates a request envelope with the ID of the originating request.
  /// </summary>
  /// <typeparam name="TRequest">The request message type.</typeparam>
  /// <typeparam name="TResponse">The response type expected for the request.</typeparam>
  public Message ToEnvelope<TRequest, TResponse>(
    TRequest message,
    RequestId requestId
  ) where TRequest : IRequest<TResponse> where TResponse : IResponse;

  /// <summary>
  /// Creates a response envelope linked to the originating request.
  /// </summary>
  /// <typeparam name="TResponse">The response message type.</typeparam>
  public Message ToEnvelope<TResponse>(
    TResponse message,
    RequestId replyTo
  ) where TResponse : IResponse;

  /// <summary>
  /// Deserializes a request envelope and verifies its request ID.
  /// </summary>
  /// <typeparam name="TRequest">The request message type.</typeparam>
  /// <typeparam name="TResponse">The response type expected for the request.</typeparam>
  public TRequest FromRequestEnvelope<TRequest, TResponse>(
    Message envelope
  ) where TRequest : IRequest<TResponse> where TResponse : IResponse;

  /// <summary>
  /// Deserializes a response envelope and verifies its ReplyTo value.
  /// </summary>
  /// <typeparam name="TResponse">The response message type.</typeparam>
  public TResponse FromResponseEnvelope<TResponse>(
    Message envelope
  ) where TResponse : IResponse;
}