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

internal sealed class MessageResponder<TResponse>(
  IMessageStream stream,
  IMessageEnvelopeConverter converter,
  RequestId requestId
) : IMessageResponder<TResponse> where TResponse : IResponse {
  public Task SendAsync( TResponse response ) {
    return stream.SendAsync( converter, response, requestId );
  }
}

internal sealed class StreamingMessageResponder<TProgress, TFinalResponse>(
  IMessageStream stream,
  IMessageEnvelopeConverter converter,
  RequestId requestId
) : IStreamingMessageResponder<TProgress, TFinalResponse>
  where TProgress : IResponse
  where TFinalResponse : IResponse {
  public Task SendAsync( TFinalResponse response ) {
    return stream.SendAsync( converter, response, requestId );
  }

  public void SendProgress( TProgress progress ) {
    stream.SendFireAndForget( converter, progress, requestId );
  }
}