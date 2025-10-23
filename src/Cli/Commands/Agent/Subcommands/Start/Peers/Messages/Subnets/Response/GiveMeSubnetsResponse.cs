using Drift.Domain;
using Drift.Networking.PeerStreaming.Messages;

namespace Drift.Cli.Commands.Agent.Subcommands.Start.Peers.Messages.Subnets.Response;

public class GiveMeSubnetsResponse : IPeerMessage {
  public string MessageType => "subnetresponse";

  public IReadOnlyList<CidrBlock> Subnets {
    get;
    set;
  }
}