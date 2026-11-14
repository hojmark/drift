using System.Text.Json.Serialization.Metadata;

namespace Drift.Networking.Core.Abstractions;

public interface IMessage {
  /// <summary>
  /// Gets the unique type identifier for the message.
  /// </summary>
  static abstract string MessageType {
    get;
  }

  /// <summary>
  /// Gets the JSON serialization contract metadata.
  /// </summary>
  static abstract JsonTypeInfo JsonInfo {
    get;
  }
}

/// <summary>
/// Represents a request associated with a specific response type.
/// </summary>
/// <typeparam name="TResponse">The response type associated with the request.</typeparam>
#pragma warning disable S2326 // Generic parameters intentionally provide type-safe request/response pairing.
public interface IRequest<TResponse> : IMessage where TResponse : IResponse;

/// <summary>
/// Represents a request that provides intermediate progress updates and a final response.
/// </summary>
/// <typeparam name="TProgress">The type of intermediate progress response.</typeparam>
/// <typeparam name="TResponse">The type of final response.</typeparam>
public interface IStreamingRequest<TProgress, TResponse> : IRequest<TResponse>
  where TProgress : IResponse
  where TResponse : IResponse;
#pragma warning restore S2326

/// <summary>
/// Represents a response.
/// </summary>
public interface IResponse : IMessage {
  static readonly Empty Empty = Empty.Instance;
}