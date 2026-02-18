using System.Collections.Concurrent;
using Drift.Networking.Grpc.Generated;
using Microsoft.Extensions.Logging;

namespace Drift.Networking.PeerStreaming.Core.Messages;

//TODO private?
public sealed class PeerResponseCorrelator {
  private readonly ConcurrentDictionary<string, TaskCompletionSource<PeerMessage>> _pendingRequests = new();
  private readonly ConcurrentDictionary<string, StreamingResponseHandler> _streamingRequests = new();
  private readonly ILogger _logger;

  public PeerResponseCorrelator( ILogger logger ) {
    _logger = logger;
  }

  public Task<PeerMessage> WaitForResponseAsync( string correlationId, TimeSpan timeout, CancellationToken ct ) {
    var tcs = new TaskCompletionSource<PeerMessage>();

    if ( !_pendingRequests.TryAdd( correlationId, tcs ) ) {
      throw new InvalidOperationException( $"Correlation ID {correlationId} already exists" );
    }

    var cts = CancellationTokenSource.CreateLinkedTokenSource( ct );
    cts.CancelAfter( timeout );

    cts.Token.Register( () => {
      if ( _pendingRequests.TryRemove( correlationId, out var removed ) ) {
        removed.TrySetCanceled();
      }
    } );

    return tcs.Task;
  }

  public Task<PeerMessage> WaitForStreamingResponseAsync(
    string correlationId,
    string finalMessageType,
    Action<PeerMessage> onProgressUpdate,
    TimeSpan timeout,
    CancellationToken ct
  ) {
    var handler = new StreamingResponseHandler {
      CompletionSource = new TaskCompletionSource<PeerMessage>(),
      FinalMessageType = finalMessageType,
      OnProgressUpdate = onProgressUpdate
    };

    if ( !_streamingRequests.TryAdd( correlationId, handler ) ) {
      throw new InvalidOperationException( $"Correlation ID {correlationId} already exists" );
    }

    var cts = CancellationTokenSource.CreateLinkedTokenSource( ct );
    cts.CancelAfter( timeout );

    cts.Token.Register( () => {
      if ( _streamingRequests.TryRemove( correlationId, out var removed ) ) {
        removed.CompletionSource.TrySetCanceled();
      }
    } );

    return handler.CompletionSource.Task;
  }

  public bool TryCompleteResponse( string correlationId, PeerMessage response ) {
    // Check for streaming response first
    if ( _streamingRequests.TryGetValue( correlationId, out var streamingHandler ) ) {
      // If this is the final message, complete the task
      if ( response.MessageType == streamingHandler.FinalMessageType ) {
        _streamingRequests.TryRemove( correlationId, out _ );
        return streamingHandler.CompletionSource.TrySetResult( response );
      }

      // Otherwise, it's a progress update
      streamingHandler.OnProgressUpdate( response );
      return true;
    }

    // Check for regular single response
    if ( _pendingRequests.TryRemove( correlationId, out var tcs ) ) {
      return tcs.TrySetResult( response );
    }

    _logger.LogWarning( "Received response for unknown correlation ID: {CorrelationId}", correlationId );
    return false;
  }

  private sealed class StreamingResponseHandler {
    public required TaskCompletionSource<PeerMessage> CompletionSource { get; init; }
    public required string FinalMessageType { get; init; }
    public required Action<PeerMessage> OnProgressUpdate { get; init; }
  }
}