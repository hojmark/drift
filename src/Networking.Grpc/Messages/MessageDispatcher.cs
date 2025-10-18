using Drift.Networking.Grpc.Generated;

namespace Drift.Networking.Grpc.Messages;

public class PeerMessageHandlerDispatcher {
  private readonly Dictionary<string, IPeerMessageHandler> _handlers;

  public PeerMessageHandlerDispatcher( IEnumerable<IPeerMessageHandler> handlers ) {
    _handlers = handlers.ToDictionary( h => h.MessageType, StringComparer.OrdinalIgnoreCase );
  }

  public Task DispatchAsync( PeerMessage message, CancellationToken ct = default ) {
    if ( _handlers.TryGetValue( message.MessageType, out var handler ) ) {
      return handler.HandleAsync( message, ct );
    }

    // Handle unknown/unregistered
    return Task.CompletedTask;
  }
}