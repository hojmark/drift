using Drift.Networking.Grpc.Generated;

namespace Drift.Networking.Grpc.Messages;

public interface IPeerMessageSerializer {
  public PeerMessage ToEnvelope( IPeerMessage message, string? requestId = null );

  public T FromEnvelope<T>( PeerMessage envelope ) where T : IPeerMessage;
}