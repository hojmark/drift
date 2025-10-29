using Drift.Networking.Grpc.Generated;
using Microsoft.Extensions.Logging;

namespace Drift.Networking.PeerStreaming.Core.Messages;

public class PeerMessageDispatcher {
  private readonly PeerResponseAwaiter _responseAwaiter;
  private readonly ILogger _logger;
  private readonly Dictionary<string, IPeerMessageHandler> _handlers;

  public PeerMessageDispatcher(
    IEnumerable<IPeerMessageHandler> handlers,
    PeerResponseAwaiter responseAwaiter,
    ILogger logger
  ) {
    _responseAwaiter = responseAwaiter;
    _logger = logger;
    _handlers = handlers.ToDictionary( h => h.MessageType, StringComparer.OrdinalIgnoreCase );
  }

  public Task DispatchAsync( PeerMessage message, PeerStream peerStream, CancellationToken ct = default ) {
    _logger.LogDebug( "Dispatching message: {Type}", message.MessageType );

    // If this is a response to a pending request, complete it
    if ( !string.IsNullOrEmpty( message.ReplyTo ) ) {
      if ( _responseAwaiter.TryCompleteResponse( message.ReplyTo, message ) ) {
        _logger.LogDebug( "Completed pending request: {CorrelationId}", message.ReplyTo );
        return Task.CompletedTask;
      }
    }

    // Otherwise, dispatch to handler
    if ( _handlers.TryGetValue( message.MessageType, out var handler ) ) {
      return handler.HandleAsync( message, peerStream, ct );
    }

    _logger.LogError( "Unknown message type: {Type}", message.MessageType );
    // Handle unknown/unregistered
    throw new NotImplementedException( "'" + message.MessageType + "' not handled" );
    return Task.CompletedTask;
  }
}