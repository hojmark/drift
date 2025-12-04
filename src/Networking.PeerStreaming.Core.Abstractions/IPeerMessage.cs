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

public interface IPeerRequest<TResponse> : IPeerMessage where TResponse : IPeerResponse;

public interface IPeerResponse : IPeerMessage {
  static readonly Empty Empty = Empty.Instance;
}