using Drift.Networking.PeerStreaming.Core.Abstractions;
using Microsoft.Extensions.Logging;

namespace Drift.Agent.PeerProtocol.Adopt;

internal sealed class AdoptRequestHandler : IPeerMessageHandler<AdoptRequestPayload, Empty> {
  private readonly ILogger _logger; // Example: inject what you need

  public string MessageType => AdoptRequestPayload.MessageType;

  public async Task<Empty> HandleAsync( AdoptRequestPayload message,
    CancellationToken cancellationToken = default ) {
    _logger.LogInformation( $"[AdoptRequest] Controller: {message.ControllerId}" );
    return IPeerResponse.Empty;
  }
}