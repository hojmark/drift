using Drift.Networking.Grpc.Generated;

namespace Drift.Networking.PeerStreaming.Core.Abstractions;

/*public interface IPeerMessageHandler {
  string MessageType {
    get;
  }

  Task<IPeerMessage?> HandleAsync(
    PeerMessage envelope,
    IPeerMessageEnvelopeConverter envelopeConverter,
    CancellationToken cancellationToken = default
  );
}*/

public interface IPeerMessageHandlerBase {
  string MessageType {
    get;
  }

  Task<PeerMessage?> DispatchAsync(
    PeerMessage envelope,
    IPeerMessageEnvelopeConverter converter,
    CancellationToken cancellationToken
  );
}

public interface IPeerMessageHandler<TRequest, TResponse> : IPeerMessageHandlerBase
  where TRequest : IPeerMessage
  where TResponse : IPeerMessage {
  // TODO unused now
  async Task<TResponse?> HandleAsync(
    PeerMessage envelope,
    IPeerMessageEnvelopeConverter envelopeConverter,
    CancellationToken cancellationToken
  ) {
    var typedMessage = envelopeConverter.FromEnvelope<TRequest>( envelope );
    return await HandleAsync( typedMessage, cancellationToken );
  }

  Task<TResponse?> HandleAsync( TRequest message, CancellationToken cancellationToken = default );

  async Task<PeerMessage?> IPeerMessageHandlerBase.DispatchAsync(
    PeerMessage envelope,
    IPeerMessageEnvelopeConverter converter,
    CancellationToken cancellationToken ) {
    // Deserialize strongly typed request
    var request = converter.FromEnvelope<TRequest>( envelope );

    // Process request
    var response = await HandleAsync( request, cancellationToken );

    if ( response is null )
      return null;

    // Serialize strongly typed response
    return converter.ToEnvelope<TResponse>( response );
  }
}