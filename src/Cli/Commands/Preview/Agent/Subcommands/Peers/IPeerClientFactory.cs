using Drift.Networking.Grpc.Generated;
using Grpc.Net.Client;

namespace Drift.Cli.Commands.Preview.Agent.Subcommands.Peers;

public interface IPeerClientFactory {
  (PeerService.PeerServiceClient Client, GrpcChannel Channel) Create( Uri address );
}