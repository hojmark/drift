using Drift.Networking.PeerStreaming.Core.Abstractions;

namespace Drift.Agent.PeerProtocol.Adopt;

internal sealed class AdoptRequestPayload : IPeerMessage {
  public string MessageType => "adopt-request";

  public string Jwt {
    get;
    set;
  }

  public string ControllerId {
    get;
    set;
  }
}