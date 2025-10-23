using Drift.Networking.Grpc.Messages;

namespace Drift.Cli.Commands.Agent.Subcommands.Start.Peers.Messages.Subnets;

public class GiveMeSubnets : IPeerMessage {
  public string MessageType => "give_me_subnets";
}