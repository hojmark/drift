using Drift.Domain;

namespace Drift.Networking.Grpc.Messages;

public interface IPeerMessage {
  string MessageType {
    get;
  }
}