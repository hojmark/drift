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

public interface IPeerRequestMessage<TResponse> : IPeerMessage where TResponse : IPeerResponseMessage;

public interface IPeerResponseMessage : IPeerMessage;