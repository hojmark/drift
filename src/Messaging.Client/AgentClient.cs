using Drift.Domain;
using Drift.Networking.Core.Abstractions;
using Microsoft.Extensions.Logging;

namespace Drift.Messaging.Client;

internal sealed class AgentClient(
  IMessageEnvelopeConverter envelopeConverter,
  IMessageStreamManager messageStreamManager,
  ILogger logger,
  AgentClientOptions? options = null
) : IAgentClient {
  private readonly AgentClientOptions _options = options ?? new AgentClientOptions();

  public async Task<TResponse> RequestAsync<TRequest, TResponse>(
    Domain.Agent agent,
    TRequest message,
    TimeSpan? timeout = null,
    CancellationToken cancellationToken = default
  ) where TResponse : IResponse where TRequest : IRequest<TResponse> {
    return await ExecuteWithRetryAsync(
      agent,
      async () => await RequestInternalAsync<TRequest, TResponse>( agent, message, timeout, cancellationToken ),
      cancellationToken
    );
  }

  private async Task<TResponse> RequestInternalAsync<TRequest, TResponse>(
    Domain.Agent agent,
    TRequest message,
    TimeSpan? timeout,
    CancellationToken cancellationToken
  ) where TResponse : IResponse where TRequest : IRequest<TResponse> {
    var requestId = RequestId.New();
    var envelope = envelopeConverter.ToEnvelope<TRequest, TResponse>( message, requestId );

    // Register correlator BEFORE sending
    var connection = messageStreamManager.GetOrCreate( new Uri( agent.Address ), agent.Id );
    var responseTask = connection.WaitForResponseAsync(
      requestId,
      timeout ?? _options.DefaultTimeout,
      cancellationToken
    );

    // Request
    await connection.Stream.SendAsync( envelope );

    // Response
    var response = await responseTask;
    return envelopeConverter.FromResponseEnvelope<TResponse>( response );
  }

  public async Task<TResponse> RequestStreamingAsync<TRequest, TProgress, TResponse>(
    Domain.Agent agent,
    TRequest message,
    Action<TProgress> onProgress,
    TimeSpan? timeout = null,
    CancellationToken cancellationToken = default
  ) where TRequest : IStreamingRequest<TProgress, TResponse>
    where TProgress : IResponse
    where TResponse : IResponse {
    return await ExecuteWithRetryAsync(
      agent,
      async () => await RequestStreamingInternalAsync<TRequest, TProgress, TResponse>(
        agent,
        message,
        onProgress,
        timeout,
        cancellationToken
      ),
      cancellationToken
    );
  }

  private async Task<TResponse> RequestStreamingInternalAsync<TRequest, TProgress, TResponse>(
    Domain.Agent agent,
    TRequest message,
    Action<TProgress> onProgress,
    TimeSpan? timeout,
    CancellationToken cancellationToken
  ) where TRequest : IStreamingRequest<TProgress, TResponse>
    where TProgress : IResponse
    where TResponse : IResponse {
    var requestId = RequestId.New();
    var envelope = envelopeConverter.ToEnvelope<TRequest, TResponse>( message, requestId );

    // Register streaming correlator BEFORE sending
    var connection = messageStreamManager.GetOrCreate( new Uri( agent.Address ), agent.Id );
    var responseTask = connection.WaitForStreamingResponseAsync(
      requestId,
      TResponse.MessageType,
      progressEnvelope =>
        onProgress( envelopeConverter.FromResponseEnvelope<TProgress>( progressEnvelope ) ),
      timeout ?? _options.StreamingTimeout,
      cancellationToken
    );

    // Request
    await connection.Stream.SendAsync( envelope );

    // Final Response
    var response = await responseTask;
    return envelopeConverter.FromResponseEnvelope<TResponse>( response );
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

        LogOperationFailure(
          agent.Id,
          attempt,
          _options.MaxRetryAttempts,
          ex,
          attempt > _options.MaxRetryAttempts
        );

        if ( attempt > _options.MaxRetryAttempts ) {
          break;
        }
      }
    }

    throw new AggregateException(
      $"Operation failed for agent {agent.Id} after {attempt} attempts",
      lastException!
    );
  }

  private void LogOperationFailure(
    AgentId agentId,
    int attempt,
    int maxAttempts,
    Exception exception,
    bool exhausted
  ) {
    if ( exhausted ) {
      logger.LogDebug(
        exception,
        "Operation failed for agent {AgentId} after {Attempts} attempts: {Message}",
        agentId,
        attempt,
        exception.GetBaseException().Message
      );
      return;
    }

    logger.LogDebug(
      exception,
      "Operation failed for agent {AgentId} (attempt {Attempt}/{MaxAttempts}): {Message}",
      agentId,
      attempt,
      maxAttempts,
      exception.GetBaseException().Message
    );
  }

  private int CalculateBackoffDelay( int attempt ) {
    var delay = _options.RetryBaseDelayMs * Math.Pow( 2, attempt - 1 );
    return (int) Math.Min( delay, _options.RetryMaxDelayMs );
  }
}
