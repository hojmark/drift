using Drift.Networking.Grpc.Generated;

namespace Drift.Networking.Grpc.Messages;

public interface IPeerMessageHandler {
  string MessageType {
    get;
  }

  Task HandleAsync( PeerMessage message, CancellationToken cancellationToken = default );
}