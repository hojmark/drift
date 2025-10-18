namespace Drift.Networking.Grpc.Messages.Adopt;

public class AdoptRequestPayload
{
  public string Jwt { get; set; }
  public string ControllerId { get; set; }
}