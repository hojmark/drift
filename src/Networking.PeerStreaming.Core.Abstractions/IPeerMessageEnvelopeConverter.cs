using Drift.Networking.Grpc.Generated;

namespace Drift.Networking.PeerStreaming.Core.Abstractions;

public interface IPeerMessageEnvelopeConverter {
  public PeerMessage ToEnvelope<T>( IPeerMessage message, string? requestId = null ) where T : IPeerMessage;

  public T FromEnvelope<T>( PeerMessage envelope ) where T : IPeerMessage;
}