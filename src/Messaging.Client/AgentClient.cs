using Drift.Networking.Core.Abstractions;
using Drift.Networking.Core.Messages;
using Microsoft.Extensions.Logging;

namespace Drift.Messaging.Client;

internal sealed class AgentClient(
  IMessageEnvelopeConverter envelopeConverter,
  IMessageStreamManager messageStreamManager,
  MessageResponseCorrelator responseCorrelator,
  ILogger logger,
  AgentClientOptions? options = null
) : IAgentClient {
  private readonly AgentClientOptions _options = options ?? new AgentClientOptions();

  public async Task<TResponse> SendAndWaitAsync<TRequest, TResponse>(
    Domain.Agent agent,
    TRequest message,
    TimeSpan? timeout = null,
    CancellationToken cancellationToken = default
  ) where TResponse : IResponse where TRequest : IRequest<TResponse> {
    return await ExecuteWithRetryAsync(
      agent,
      async () => await SendAndWaitInternalAsync<TRequest, TResponse>( agent, message, timeout, cancellationToken ),
      cancellationToken
    );
  }

  private async Task<TResponse> SendAndWaitInternalAsync<TRequest, TResponse>(
    Domain.Agent agent,
    TRequest message,
    TimeSpan? timeout,
    CancellationToken cancellationToken
  ) where TResponse : IResponse where TRequest : IRequest<TResponse> {
    var correlationId = Guid.NewGuid().ToString();
    var envelope = envelopeConverter.ToEnvelope<TRequest>( message );
    envelope.CorrelationId = correlationId;

    // Register correlator BEFORE sending
    var responseTask = responseCorrelator.WaitForResponseAsync(
      correlationId,
      timeout ?? _options.DefaultTimeout,
      cancellationToken
    );

    // Request
    var connection = messageStreamManager.GetOrCreate( new Uri( agent.Address ), agent.Id );
    await connection.SendAsync( envelope );

    // Response
    var response = await responseTask;
    return envelopeConverter.FromEnvelope<TResponse>( response );
  }

  public async Task<TFinalResponse> SendAndWaitStreamingAsync<TRequest, TFinalResponse>(
    Domain.Agent agent,
    TRequest message,
    string finalMessageType,
    Action<Drift.Networking.Grpc.Generated.Message> onProgressUpdate,
    TimeSpan? timeout = null,
    CancellationToken cancellationToken = default
  ) where TFinalResponse : IResponse where TRequest : IMessage {
    return await ExecuteWithRetryAsync(
      agent,
      async () => await SendAndWaitStreamingInternalAsync<TRequest, TFinalResponse>(
        agent,
        message,
        finalMessageType,
        onProgressUpdate,
        timeout,
        cancellationToken
      ),
      cancellationToken
    );
  }

  private async Task<TFinalResponse> SendAndWaitStreamingInternalAsync<TRequest, TFinalResponse>(
    Domain.Agent agent,
    TRequest message,
    string finalMessageType,
    Action<Drift.Networking.Grpc.Generated.Message> onProgressUpdate,
    TimeSpan? timeout,
    CancellationToken cancellationToken
  ) where TFinalResponse : IResponse where TRequest : IMessage {
    var correlationId = Guid.NewGuid().ToString();
    var envelope = envelopeConverter.ToEnvelope<TRequest>( message );
    envelope.CorrelationId = correlationId;

    // Register streaming correlator BEFORE sending
    var responseTask = responseCorrelator.WaitForStreamingResponseAsync(
      correlationId,
      finalMessageType,
      onProgressUpdate,
      timeout ?? _options.StreamingTimeout,
      cancellationToken
    );

    // Request
    var connection = messageStreamManager.GetOrCreate( new Uri( agent.Address ), agent.Id );
    await connection.SendAsync( envelope );

    // Final Response
    var response = await responseTask;
    return envelopeConverter.FromEnvelope<TFinalResponse>( response );
  }

  private async Task<TResult> ExecuteWithRetryAsync<TResult>(
    Domain.Agent agent,
    Func<Task<TResult>> operation,
    CancellationToken cancellationToken
  ) {
    var attempt = 0;
    Exception? lastException = null;

    while ( attempt <= _options.MaxRetryAttempts ) {
      try {
        if ( attempt > 0 ) {
          var delay = CalculateBackoffDelay( attempt );
          logger.LogDebug(
            "Retrying operation for agent {AgentId} (attempt {Attempt}/{MaxAttempts}) after {Delay}ms",
            agent.Id,
            attempt,
            _options.MaxRetryAttempts,
            delay
          );
          await Task.Delay( delay, cancellationToken );
        }

        return await operation();
      }
      catch ( OperationCanceledException ) {
        // Don't retry on cancellation
        throw;
      }
      catch ( Exception ex ) {
        lastException = ex;
        attempt++;

        if ( attempt > _options.MaxRetryAttempts ) {
          logger.LogError(
            ex,
            "Operation failed for agent {AgentId} after {Attempts} attempts",
            agent.Id,
            attempt
          );
          break;
        }

        logger.LogWarning(
          ex,
          "Operation failed for agent {AgentId} (attempt {Attempt}/{MaxAttempts}): {Message}",
          agent.Id,
          attempt,
          _options.MaxRetryAttempts,
          ex.Message
        );
      }
    }

    throw new AggregateException(
      $"Operation failed for agent {agent.Id} after {attempt} attempts",
      lastException!
    );
  }

  private int CalculateBackoffDelay( int attempt ) {
    var delay = _options.RetryBaseDelayMs * Math.Pow( 2, attempt - 1 );
    return (int) Math.Min( delay, _options.RetryMaxDelayMs );
  }
}