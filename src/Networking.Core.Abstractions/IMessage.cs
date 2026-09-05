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

/// <summary>
/// Marks a message as a request and associates it with its response type.
/// </summary>
/// <typeparam name="TResponse">The response type associated with the request.</typeparam>
#pragma warning disable S2326 // Generic parameters intentionally provide type-safe request/response pairing.
public interface IRequest<TResponse> : IMessage where TResponse : IResponse;

/// <summary>
/// Marks a request that produces intermediate progress responses and a final response.
/// </summary>
/// <typeparam name="TProgress">The type of intermediate progress response.</typeparam>
/// <typeparam name="TResponse">The type of final response.</typeparam>
public interface IStreamingRequest<TProgress, TResponse> : IRequest<TResponse>
  where TProgress : IResponse
  where TResponse : IResponse;
#pragma warning restore S2326

public interface IResponse : IMessage {
  static readonly Empty Empty = Empty.Instance;
}