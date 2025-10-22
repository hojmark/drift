using Drift.Networking.Grpc.Generated;
using Drift.Networking.Grpc.Messages;
using Drift.Scanning.Subnets.Interface;

namespace Drift.Cli.Commands.Preview.Agent.Subcommands.Peers.Messages;

internal class GiveMeSubnetsHandler(
  IInterfaceSubnetProvider interfaceSubnetProvider
  //PeerMessageDispatcher dispatcher
) : IPeerMessageHandler {
  public string? MessageType => "give_me_subnets";

  public Task HandleAsync( PeerMessage message, CancellationToken cancellationToken = default ) {
    throw new NotImplementedException( "dispatch the subnets" );
  }
}