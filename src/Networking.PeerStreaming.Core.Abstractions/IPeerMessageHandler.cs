using Drift.Networking.Grpc.Generated;

namespace Drift.Networking.PeerStreaming.Core.Abstractions;

public interface IPeerMessageHandler {
  string? MessageType {
    get;
  }

  Task HandleAsync( PeerMessage message, IPeerStream peerStream, CancellationToken cancellationToken = default );
}