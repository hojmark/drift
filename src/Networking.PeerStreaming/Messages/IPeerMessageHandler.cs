using Drift.Networking.Grpc.Generated;

namespace Drift.Networking.PeerStreaming.Messages;

public interface IPeerMessageHandler {
  string? MessageType {
    get;
  }

  Task HandleAsync( PeerMessage message, PeerStream peerStream, CancellationToken cancellationToken = default );
}