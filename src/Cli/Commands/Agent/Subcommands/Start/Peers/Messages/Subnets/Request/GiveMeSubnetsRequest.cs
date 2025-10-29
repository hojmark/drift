using Drift.Networking.PeerStreaming.Core.Messages;

namespace Drift.Cli.Commands.Agent.Subcommands.Start.Peers.Messages.Subnets.Request;

public class GiveMeSubnetsRequest : IPeerMessage {
  public string MessageType => "subnetrequest";
}