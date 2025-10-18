namespace Drift.Networking.Grpc.Messages.Adopt;

public interface IAdoptRequestHandler {
  public string MessageType => "adopt-request";

  Task HandleAsync( AdoptRequestPayload message, CancellationToken cancellationToken = default );
}