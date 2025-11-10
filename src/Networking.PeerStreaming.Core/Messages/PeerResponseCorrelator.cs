using System.Collections.Concurrent;
using Drift.Networking.Grpc.Generated;
using Microsoft.Extensions.Logging;

namespace Drift.Networking.PeerStreaming.Core.Messages;

//TODO private?
public sealed class PeerResponseCorrelator {
  private readonly ConcurrentDictionary<string, TaskCompletionSource<PeerMessage>> _pendingRequests = new();
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

  public bool TryCompleteResponse( string correlationId, PeerMessage response ) {
    if ( _pendingRequests.TryRemove( correlationId, out var tcs ) ) {
      return tcs.TrySetResult( response );
    }

    _logger.LogWarning( "Received response for unknown correlation ID: {CorrelationId}", correlationId );
    return false;
  }
}