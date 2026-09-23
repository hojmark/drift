using Drift.Domain;

namespace Drift.Networking.Core.Abstractions;

public interface IMessageResponder<in TResponse> where TResponse : IResponse {
  Task SendAsync( TResponse response );
}

public interface IStreamingMessageResponder<in TProgress, in TResponse>
  : IMessageResponder<TResponse>
  where TProgress : IResponse
  where TResponse : IResponse {
  void SendProgress( TProgress progress );
}

internal class MessageResponder<TResponse>(
  IMessageStream stream,
  IMessageEnvelopeConverter converter,
  RequestId requestId
) : IMessageResponder<TResponse> where TResponse : IResponse {
  public Task SendAsync( TResponse response ) {
    return stream.SendAsync( converter, response, requestId );
  }

  protected void SendFireAndForget<TMessage>( TMessage message ) where TMessage : IResponse {
    stream.SendFireAndForget( converter, message, requestId );
  }
}

internal sealed class StreamingMessageResponder<TProgress, TResponse>(
  IMessageStream stream,
  IMessageEnvelopeConverter converter,
  RequestId requestId
) : MessageResponder<TResponse>( stream, converter, requestId ), IStreamingMessageResponder<TProgress, TResponse>
  where TProgress : IResponse
  where TResponse : IResponse {
  public void SendProgress( TProgress progress ) {
    SendFireAndForget( progress );
  }
}