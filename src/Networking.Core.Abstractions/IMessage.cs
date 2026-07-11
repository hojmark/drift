using System.Text.Json.Serialization.Metadata;

namespace Drift.Networking.Core.Abstractions;

public interface IMessage {
  static abstract string MessageType {
    get;
  }

  static abstract JsonTypeInfo JsonInfo {
    get;
  }
}

#pragma warning disable S2326 // TResponse is an intentional phantom type parameter for type-safe request/response pairing
public interface IRequest<TResponse> : IMessage where TResponse : IResponse;
#pragma warning restore S2326

public interface IResponse : IMessage {
  static readonly Empty Empty = Empty.Instance;
}