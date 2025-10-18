using System.Text.Json;
using Drift.Networking.Grpc.Generated;

namespace Drift.Networking.Grpc.Messages;

public abstract class HandlerBase<TMsg> : IPeerMessageHandler {
  public abstract string MessageType {
    get;
  }

  public Task HandleAsync( PeerMessage message, CancellationToken cancellationToken = default ) {
    var payload = JsonSerializer.Deserialize<TMsg>( message.Message );
    return HandleAsync( payload, cancellationToken );
  }

  protected abstract Task HandleAsync( TMsg message, CancellationToken cancellationToken = default );
}