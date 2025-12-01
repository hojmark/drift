using System.Text.Json.Serialization.Metadata;

namespace Drift.Networking.PeerStreaming.Core.Abstractions;

public interface IPeerMessage {
  static abstract string MessageType {
    get;
  }

  static abstract JsonTypeInfo JsonInfo {
    get;
  }
}