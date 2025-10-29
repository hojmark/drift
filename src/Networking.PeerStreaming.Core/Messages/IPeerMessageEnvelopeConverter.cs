using Drift.Networking.Grpc.Generated;

namespace Drift.Networking.PeerStreaming.Core.Messages;

public interface IPeerMessageEnvelopeConverter {
  public PeerMessage ToEnvelope( IPeerMessage message, string? requestId = null );

  public T FromEnvelope<T>( PeerMessage envelope ) where T : IPeerMessage;
}