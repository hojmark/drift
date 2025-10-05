namespace Drift.Agent.PeerProtocol.Adopt;

internal interface IAdoptRequestHandler {
  public string MessageType => "adopt-request";

  Task HandleAsync( AdoptRequestPayload message, CancellationToken cancellationToken = default );
}