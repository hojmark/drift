namespace Drift.Networking.Core.Abstractions;

public static class MessageHandlerExtensions {
  /// <summary>
  /// Helper to send a response with the correct correlation ID set.
  /// </summary>
  /// <typeparam name="TResponse">The type of the response message to send.</typeparam>
  public static async Task SendAsync<TResponse>(
    this IMessageStream stream,
    IMessageEnvelopeConverter converter,
    TResponse response,
    string correlationId
  ) where TResponse : IMessage {
    var envelope = converter.ToEnvelope<TResponse>( response );
    envelope.ReplyTo = correlationId;
    await stream.SendAsync( envelope );
  }

  /// <summary>
  /// Helper to send a response without awaiting (fire and forget).
  /// Useful for progress updates that shouldn't block processing.
  /// </summary>
  /// <typeparam name="TResponse">The type of the response message to send.</typeparam>
  public static void SendFireAndForget<TResponse>(
    this IMessageStream stream,
    IMessageEnvelopeConverter converter,
    TResponse response,
    string correlationId
  ) where TResponse : IMessage {
    var envelope = converter.ToEnvelope<TResponse>( response );
    envelope.ReplyTo = correlationId;
    _ = stream.SendAsync( envelope ).ContinueWith(
      t => {
        t.Exception?.Handle( ex => {
          // TODO: Handle exception
          return true;
        } );
      },
      TaskContinuationOptions.OnlyOnFaulted
    );
  }
}