using Drift.Networking.Grpc.Messages;

namespace Drift.Cli.Commands.Preview.Agent.Subcommands.Peers.Messages;

public class GiveMeSubnets : IPeerMessage {
  public string MessageType => "give_me_subnets";
}