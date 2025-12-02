using Drift.Networking.Grpc.Generated;

namespace Drift.Networking.PeerStreaming.Core.Abstractions;

public interface IPeerMessageHandler {
  /// <summary>
  /// Gets the message type name that this handler can process.
  /// </summary>
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
  where TRequest : IPeerRequestMessage<TResponse>
  where TResponse : IPeerResponseMessage {
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