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

public interface IPeerMessageHandler {
  string MessageType {
    get;
  }

  Task<PeerMessage?> HandleAsync(
    PeerMessage envelope,
    IPeerMessageEnvelopeConverter converter,
    CancellationToken cancellationToken
  );
}

public interface IPeerMessageHandler<TRequest, TResponse> : IPeerMessageHandler
  where TRequest : IPeerMessage
  where TResponse : IPeerMessage {
  Task<TResponse?> HandleAsync( TRequest message, CancellationToken cancellationToken = default );

  async Task<PeerMessage?> IPeerMessageHandler.HandleAsync(
    PeerMessage envelope,
    IPeerMessageEnvelopeConverter converter,
    CancellationToken cancellationToken ) {
    var request = converter.FromEnvelope<TRequest>( envelope );

    var response = await HandleAsync( request, cancellationToken );

    if ( response is null ) {
      return null;
    }

    return converter.ToEnvelope<TResponse>( response );
  }
}