using Drift.Networking.PeerStreaming.Core.Abstractions;
using Drift.Networking.PeerStreaming.Core.Messages;
using Microsoft.Extensions.Logging;

namespace Drift.Networking.Cluster;

internal sealed class Cluster(
  IPeerMessageEnvelopeConverter envelopeConverter,
  IPeerStreamManager peerStreamManager,
  PeerResponseCorrelator responseCorrelator,
  ILogger logger,
  ClusterOptions? options = null
) : ICluster {
  private readonly ClusterOptions _options = options ?? new ClusterOptions();
  /*public async Task SendAsync<TMessage>(
    Domain.Agent agent,
    TMessage message,
    CancellationToken cancellationToken = default
  ) where TMessage : IPeerMessage {
    try {
      await SendInternalAsync<TMessage>( agent, message, cancellationToken );
    }
    catch ( Exception ex ) {
      logger.LogWarning( ex, "Send to {Peer} failed", agent );
    }
  }*/

  /* public async Task BroadcastAsync( PeerMessage message, CancellationToken cancellationToken = default ) {
     var peers = peerStreamManager.GetConnectedPeers();

     var tasks = peers.Select( async peer => {
       // TODO optimistically assume connection is alive, but automatically reconnect if it's not
       try {
         await SendInternalAsync( peer, message, cancellationToken );
       }
       catch ( Exception ex ) {
         logger.LogWarning( ex, "Broadcast to {Peer} failed", peer );
       }
     } );

     await Task.WhenAll( tasks );
   }*/

  /*public async Task SendInternalAsync<TMessage>(
    Domain.Agent agent,
    TMessage message,
    CancellationToken cancellationToken = default
  ) where TMessage : IPeerMessage {
    var connection = peerStreamManager.GetOrCreate( new Uri( agent.Address ), "agentid_local1" );
    var envelope = envelopeConverter.ToEnvelope<TMessage>( message );
    await connection.SendAsync( envelope );
  }*/

  public async Task<TResponse> SendAndWaitAsync<TRequest, TResponse>(
    Domain.Agent agent,
    TRequest message,
    TimeSpan? timeout = null,
    CancellationToken cancellationToken = default
  ) where TResponse : IPeerResponse where TRequest : IPeerRequest<TResponse> {
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
  ) where TResponse : IPeerResponse where TRequest : IPeerRequest<TResponse> {
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
    var connection = peerStreamManager.GetOrCreate( new Uri( agent.Address ), agent.Id );
    await connection.SendAsync( envelope );

    // Response
    var response = await responseTask;
    return envelopeConverter.FromEnvelope<TResponse>( response );
  }

  public async Task<TFinalResponse> SendAndWaitStreamingAsync<TRequest, TFinalResponse>(
    Domain.Agent agent,
    TRequest message,
    string finalMessageType,
    Action<Drift.Networking.Grpc.Generated.PeerMessage> onProgressUpdate,
    TimeSpan? timeout = null,
    CancellationToken cancellationToken = default
  ) where TFinalResponse : IPeerResponse where TRequest : IPeerMessage {
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
    Action<Drift.Networking.Grpc.Generated.PeerMessage> onProgressUpdate,
    TimeSpan? timeout,
    CancellationToken cancellationToken
  ) where TFinalResponse : IPeerResponse where TRequest : IPeerMessage {
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
    var connection = peerStreamManager.GetOrCreate( new Uri( agent.Address ), agent.Id );
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

    // All retries exhausted
    throw new AggregateException(
      $"Operation failed for agent {agent.Id} after {attempt} attempts",
      lastException!
    );
  }

  private int CalculateBackoffDelay( int attempt ) {
    // Exponential backoff: base * 2^(attempt-1)
    var delay = _options.RetryBaseDelayMs * Math.Pow( 2, attempt - 1 );
    return (int)Math.Min( delay, _options.RetryMaxDelayMs );
  }
}