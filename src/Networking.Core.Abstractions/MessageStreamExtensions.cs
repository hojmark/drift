namespace Drift.Networking.Core.Abstractions;

using Drift.Domain;

public static class MessageStreamExtensions {
  extension( IMessageStream stream ) {
    /// <summary>
    /// Sends a response correlated to the request identified by <paramref name="requestId"/>.
    /// </summary>
    /// <seealso cref="MessageStreamExtensions.SendFireAndForget"/>
    public async Task SendAsync<TResponse>(
      IMessageEnvelopeConverter converter,
      TResponse response,
      RequestId requestId
    ) where TResponse : IResponse {
      var envelope = converter.ToEnvelope( response, requestId );
      await stream.SendAsync( envelope );
    }

    /// <summary>
    /// Sends a response correlated to the request identified by <paramref name="requestId"/> <i>without awaiting the transport write</i>.
    /// </summary>
    /// <seealso cref="MessageStreamExtensions.SendAsync"/>
    public void SendFireAndForget<TResponse>(
      IMessageEnvelopeConverter converter,
      TResponse response,
      RequestId requestId
    ) where TResponse : IResponse {
      var envelope = converter.ToEnvelope( response, requestId );
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
}