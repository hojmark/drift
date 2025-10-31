using Drift.Domain;
using Drift.Networking.PeerStreaming.Core.Abstractions;

namespace Drift.Cli.Commands.Agent.Subcommands.Start.Peers.Messages.Subnets.Response;

public class SubnetsResponse : IPeerMessage {
  public string MessageType => "subnetsresponse";

  public required IReadOnlyList<CidrBlock> Subnets {
    get;
    init;
  }
}