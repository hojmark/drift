using Drift.Networking.PeerStreaming.Core.Abstractions;

namespace Drift.Cli.Commands.Agent.Subcommands.Start.Peers.Messages.Subnets.Request;

public class SubnetsRequest : IPeerMessage {
  public string MessageType => "subnetsrequest";
}