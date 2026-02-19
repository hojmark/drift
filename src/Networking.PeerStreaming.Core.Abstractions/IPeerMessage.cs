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

#pragma warning disable S2326 // TResponse is an intentional phantom type parameter for type-safe request/response pairing
public interface IPeerRequest<TResponse> : IPeerMessage where TResponse : IPeerResponse;
#pragma warning restore S2326

public interface IPeerResponse : IPeerMessage {
  static readonly Empty Empty = Empty.Instance;
}