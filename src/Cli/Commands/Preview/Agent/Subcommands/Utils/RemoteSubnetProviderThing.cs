using Drift.Networking.Grpc.Generated;
using Drift.Networking.Grpc.Messages;
using Drift.Scanning.Subnets.Interface;

namespace Drift.Cli.Commands.Preview.Agent.Subcommands.Utils;

public class RemoteSubnetProviderThing( IInterfaceSubnetProvider subnetProvider )
  : IPeerMessageHandler {
  public string MessageType {
    get;
  } = "remote_subnet_provider_thing";

  public async Task HandleAsync( PeerMessage message, CancellationToken cancellationToken = default ) {
    //respond with subnets
  }
}