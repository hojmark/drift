using Drift.Networking.Grpc.Generated;
using Drift.Networking.PeerStreaming.Core.Abstractions;
using Microsoft.Extensions.Logging;

namespace Drift.Agent.PeerProtocol.Adopt;

internal sealed class AdoptRequestHandler : IPeerMessageHandler {
  private readonly ILogger _logger; // Example: inject what you need

  public string MessageType => AdoptRequestPayload.MessageType;

  public Task HandleAsync(
    PeerMessage envelope,
    IPeerMessageEnvelopeConverter converter,
    IPeerStream stream,
    CancellationToken cancellationToken
  ) {
    var message = converter.FromEnvelope<AdoptRequestPayload>( envelope );
    _logger.LogInformation( "[AdoptRequest] Controller: {ControllerId}", message.ControllerId );
    
    // This handler doesn't send a response (Empty response pattern)
    return Task.CompletedTask;
  }
}