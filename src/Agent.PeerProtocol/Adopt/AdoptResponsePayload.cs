namespace Drift.Agent.PeerProtocol.Adopt;

internal sealed class AdoptResponsePayload {
  public required string Status {
    get;
    set;
  } // "accepted" or "rejected"

  public required string AgentId {
    get;
    set;
  } // Only with "accepted"

  public required string Reason {
    get;
    set;
  } // Only with "rejected"
}
