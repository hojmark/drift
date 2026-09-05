using System.Collections.Concurrent;
using Drift.Domain;
using Drift.Networking.Grpc.Generated;
using Microsoft.Extensions.Logging;

namespace Drift.Networking.Core.Messages;

// TODO private?
public sealed class MessageResponseCorrelator( ILogger logger ) {
  private readonly ConcurrentDictionary<RequestId, TaskCompletionSource<Message>> _pendingRequests = new();
  private readonly ConcurrentDictionary<RequestId, StreamingResponseHandler> _streamingRequests = new();

  public Task<Message> WaitForResponseAsync( RequestId requestId, TimeSpan timeout, CancellationToken ct ) {
    var tcs = new TaskCompletionSource<Message>();

    if ( !_pendingRequests.TryAdd( requestId, tcs ) ) {
      throw new InvalidOperationException( $"Request ID {requestId} already exists" );
    }

    var cts = CancellationTokenSource.CreateLinkedTokenSource( ct );
    cts.CancelAfter( timeout );

    cts.Token.Register( () => {
      if ( _pendingRequests.TryRemove( requestId, out var removed ) ) {
        removed.TrySetCanceled( cts.Token );
      }

      cts.Dispose();
    } );

    return tcs.Task;
  }

  public Task<Message> WaitForStreamingResponseAsync(
    RequestId requestId,
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

    if ( !_streamingRequests.TryAdd( requestId, handler ) ) {
      throw new InvalidOperationException( $"Request ID {requestId} already exists" );
    }

    var cts = CancellationTokenSource.CreateLinkedTokenSource( ct );
    cts.CancelAfter( timeout );

    cts.Token.Register( () => {
      if ( _streamingRequests.TryRemove( requestId, out var removed ) ) {
        removed.CompletionSource.TrySetCanceled( cts.Token );
      }

      cts.Dispose();
    } );

    return handler.CompletionSource.Task;
  }

  public bool TryCompleteResponse( RequestId requestId, Message response ) {
    // Check for streaming response first
    if ( _streamingRequests.TryGetValue( requestId, out var streamingHandler ) ) {
      // If this is the final message, complete the task
      if ( response.MessageType == streamingHandler.FinalMessageType ) {
        _streamingRequests.TryRemove( requestId, out _ );
        return streamingHandler.CompletionSource.TrySetResult( response );
      }

      // Otherwise, it's a progress update
      streamingHandler.OnProgressUpdate( response );
      return true;
    }

    // Check for regular single response
    if ( _pendingRequests.TryRemove( requestId, out var tcs ) ) {
      return tcs.TrySetResult( response );
    }

    logger.LogWarning( "Received response for unknown request ID: {RequestId}", requestId );
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