using System.Collections.Concurrent;
using Drift.Networking.Grpc.Generated;
using Microsoft.Extensions.Logging;

namespace Drift.Networking.Core.Messages;

// TODO private?
public sealed class MessageResponseCorrelator( ILogger logger ) {
  private readonly ConcurrentDictionary<string, TaskCompletionSource<Message>> _pendingRequests = new();
  private readonly ConcurrentDictionary<string, StreamingResponseHandler> _streamingRequests = new();

  public Task<Message> WaitForResponseAsync( string correlationId, TimeSpan timeout, CancellationToken ct ) {
    var tcs = new TaskCompletionSource<Message>();

    if ( !_pendingRequests.TryAdd( correlationId, tcs ) ) {
      throw new InvalidOperationException( $"Correlation ID {correlationId} already exists" );
    }

    var cts = CancellationTokenSource.CreateLinkedTokenSource( ct );
    cts.CancelAfter( timeout );

    cts.Token.Register( () => {
      if ( _pendingRequests.TryRemove( correlationId, out var removed ) ) {
        removed.TrySetCanceled();
      }

      cts.Dispose();
    } );

    return tcs.Task;
  }

  public Task<Message> WaitForStreamingResponseAsync(
    string correlationId,
    string finalMessageType,
    Action<Message> onProgressUpdate,
    TimeSpan timeout,
    CancellationToken ct
  ) {
    var handler = new StreamingResponseHandler {
      CompletionSource = new TaskCompletionSource<Message>(),
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

      cts.Dispose();
    } );

    return handler.CompletionSource.Task;
  }

  public bool TryCompleteResponse( string correlationId, Message response ) {
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

    logger.LogWarning( "Received response for unknown correlation ID: {CorrelationId}", correlationId );
    return false;
  }

  private sealed class StreamingResponseHandler {
    public required TaskCompletionSource<Message> CompletionSource {
      get;
      init;
    }

    public required string FinalMessageType {
      get;
      init;
    }

    public required Action<Message> OnProgressUpdate {
      get;
      init;
    }
  }
}