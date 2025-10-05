using Drift.Domain;
using Drift.Networking.PeerStreaming.Core.Abstractions;

namespace Drift.Agent.PeerProtocol.Subnets;

public sealed class SubnetsResponse : IPeerMessage {
  public string MessageType => "subnetsresponse";

  public required IReadOnlyList<CidrBlock> Subnets {
    get;
    init;
  }
}