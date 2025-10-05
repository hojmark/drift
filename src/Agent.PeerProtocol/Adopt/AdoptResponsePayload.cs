namespace Drift.Agent.PeerProtocol.Adopt;

internal sealed class AdoptResponsePayload {
  public string Status {
    get;
    set;
  } // "accepted" or "rejected"

  public string AgentId {
    get;
    set;
  } // Only with "accepted"

  public string Reason {
    get;
    set;
  } // Only with "rejected"
}