using Drift.Networking.Grpc.Generated;

namespace Drift.Networking.PeerStreaming.Core.Abstractions;

public static class PeerMessageHandlerExtensions {
  /// <summary>
  /// Helper to send a response with the correct correlation ID set.
  /// </summary>
  public static async Task SendResponseAsync<TResponse>(
    this IPeerStream stream,
    IPeerMessageEnvelopeConverter converter,
    TResponse response,
    string correlationId
  ) where TResponse : IPeerMessage {
    var envelope = converter.ToEnvelope<TResponse>( response );
    envelope.ReplyTo = correlationId;
    await stream.SendAsync( envelope );
  }

  /// <summary>
  /// Helper to send a response without awaiting (fire and forget).
  /// Useful for progress updates that shouldn't block processing.
  /// </summary>
  public static void SendResponseFireAndForget<TResponse>(
    this IPeerStream stream,
    IPeerMessageEnvelopeConverter converter,
    TResponse response,
    string correlationId
  ) where TResponse : IPeerMessage {
    var envelope = converter.ToEnvelope<TResponse>( response );
    envelope.ReplyTo = correlationId;
    _ = stream.SendAsync( envelope );
  }
}
