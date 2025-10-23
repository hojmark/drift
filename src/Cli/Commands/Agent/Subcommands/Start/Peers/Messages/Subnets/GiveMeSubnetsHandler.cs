using Drift.Networking.Grpc.Generated;
using Drift.Networking.Grpc.Messages;
using Drift.Scanning.Subnets.Interface;

namespace Drift.Cli.Commands.Agent.Subcommands.Start.Peers.Messages.Subnets;

internal class GiveMeSubnetsHandler(
  IInterfaceSubnetProvider interfaceSubnetProvider
  //PeerMessageDispatcher dispatcher
) : IPeerMessageHandler {
  public string? MessageType => "give_me_subnets";

  public Task HandleAsync( PeerMessage message, CancellationToken cancellationToken = default ) {
    throw new NotImplementedException( "dispatch the subnets" );
  }
}