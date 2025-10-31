using System.Text.Json;
using Drift.Networking.Grpc.Generated;
using Drift.Networking.PeerStreaming.Core.Abstractions;

namespace Drift.Cli.Commands.Agent.Subcommands.Start.Peers;

internal abstract class HandlerBase<T> : IPeerMessageHandler {
  public abstract string? MessageType {
    get;
  }

  public Task HandleAsync( PeerMessage message, IPeerStream peerStream, CancellationToken cancellationToken = default ) {
    var payload = JsonSerializer.Deserialize<T>( message.Message );
    return HandleAsync( payload, cancellationToken );
  }

  protected abstract Task HandleAsync( T? payload, CancellationToken cancellationToken );
}