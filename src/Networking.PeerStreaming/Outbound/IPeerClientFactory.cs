using Drift.Networking.Grpc.Generated;
using Grpc.Net.Client;

namespace Drift.Networking.PeerStreaming.Outbound;

public interface IPeerClientFactory {
  (PeerService.PeerServiceClient Client, GrpcChannel Channel) Create( Uri address );
}