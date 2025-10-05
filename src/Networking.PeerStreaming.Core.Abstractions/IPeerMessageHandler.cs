using Drift.Networking.Grpc.Generated;

namespace Drift.Networking.PeerStreaming.Core.Abstractions;

public interface IPeerMessageHandler {
  string MessageType {
    get;
  }

  Task<IPeerMessage?> HandleAsync(
    PeerMessage envelope,
    IPeerMessageEnvelopeConverter envelopeConverter,
    CancellationToken cancellationToken = default
  );
}

public interface IPeerMessageHandler<T> : IPeerMessageHandler where T : IPeerMessage {
  async Task<IPeerMessage?> IPeerMessageHandler.HandleAsync(
    PeerMessage envelope,
    IPeerMessageEnvelopeConverter envelopeConverter,
    CancellationToken cancellationToken
  ) {
    var typedMessage = envelopeConverter.FromEnvelope<T>( envelope );
    return await HandleAsync( typedMessage, cancellationToken );
  }

  Task<IPeerMessage?> HandleAsync( T message, CancellationToken cancellationToken = default );
}