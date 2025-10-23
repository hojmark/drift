using Drift.Networking.Grpc.Generated;
using Microsoft.Extensions.Logging;

namespace Drift.Networking.Grpc.Messages;

public class PeerMessageDispatcher {
  private readonly ILogger _logger;
  private readonly Dictionary<string, IPeerMessageHandler> _handlers;

  public PeerMessageDispatcher(
    IEnumerable<IPeerMessageHandler> handlers /*, IPeerMessageSerializer serializer */,
    ILogger logger
    ) {
    _logger = logger;
    _handlers = handlers.ToDictionary( h => h.MessageType, StringComparer.OrdinalIgnoreCase );
  }

  public Task DispatchAsync( PeerMessage message, CancellationToken ct = default ) {
    _logger.LogDebug( "Dispatching message: {Type}", message.MessageType );
    if ( _handlers.TryGetValue( message.MessageType, out var handler ) ) {
      return handler.HandleAsync( message, ct );
    }

    _logger.LogError( "Unknown message type: {Type}", message.MessageType );
    // Handle unknown/unregistered
    throw new NotImplementedException( ">" + message.MessageType + " not handled<" );
    return Task.CompletedTask;
  }
}