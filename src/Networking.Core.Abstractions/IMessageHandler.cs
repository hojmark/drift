using Drift.Domain;
using Drift.Networking.Grpc.Generated;
using Microsoft.Extensions.Logging;

namespace Drift.Networking.Core.Abstractions;

public interface IMessageHandler {
  /// <summary>
  /// Gets the message type name that this handler can process.
  /// </summary>
  string MessageType {
    get;
  }

  /// TODO Should not be exposed by the application-facing handler
  /// <summary>
  /// Handles an incoming peer message. The handler is responsible for sending response(s)
  /// via the provided stream. Can send multiple responses for streaming scenarios.
  /// </summary>
  Task DispatchAsync(
    Message envelope,
    IMessageEnvelopeConverter converter,
    IMessageStream stream,
    CancellationToken cancellationToken
  );
}

public abstract class RequestHandler<TRequest, TResponse> : IMessageHandler
  where TRequest : IRequest<TResponse>
  where TResponse : IResponse {
  protected ILogger Logger {
    get;
  }

  protected RequestHandler( ILogger logger ) {
    Logger = logger;
  }

  public string MessageType => TRequest.MessageType;

  public async Task DispatchAsync(
    Message envelope,
    IMessageEnvelopeConverter converter,
    IMessageStream stream,
    CancellationToken cancellationToken
  ) {
    var request = converter.FromRequestEnvelope<TRequest, TResponse>( envelope );
    var requestId = RequestId.Parse( envelope.RequestId );

    Logger.LogInformation( "Running '{HandlerName}'", GetType().Name );

    await HandleAsync(
      request,
      new MessageResponder<TResponse>( stream, converter, requestId ),
      cancellationToken
    );
  }

  public abstract Task HandleAsync(
    TRequest request,
    IMessageResponder<TResponse> responder,
    CancellationToken cancellationToken
  );
}

public abstract class StreamingRequestHandler<TRequest, TProgress, TResponse> : IMessageHandler
  where TRequest : IStreamingRequest<TProgress, TResponse>
  where TProgress : IResponse
  where TResponse : IResponse {
  protected ILogger Logger {
    get;
  }

  protected StreamingRequestHandler( ILogger logger ) {
    Logger = logger;
  }

  public string MessageType => TRequest.MessageType;

  public async Task DispatchAsync(
    Message envelope,
    IMessageEnvelopeConverter converter,
    IMessageStream stream,
    CancellationToken cancellationToken
  ) {
    var request = converter.FromRequestEnvelope<TRequest, TResponse>( envelope );
    var requestId = RequestId.Parse( envelope.RequestId );

    Logger.LogInformation( "Running '{HandlerName}'", GetType().Name );

    await HandleAsync(
      request,
      new StreamingMessageResponder<TProgress, TResponse>( stream, converter, requestId ),
      cancellationToken
    );
  }

  public abstract Task HandleAsync(
    TRequest request,
    IStreamingMessageResponder<TProgress, TResponse> responder,
    CancellationToken cancellationToken
  );
}