using Drift.Networking.PeerStreaming.Core.Abstractions;

namespace Drift.Agent.PeerProtocol.Subnets;

public sealed class SubnetsRequest : IPeerMessage {
  public string MessageType => "subnetsrequest";
}