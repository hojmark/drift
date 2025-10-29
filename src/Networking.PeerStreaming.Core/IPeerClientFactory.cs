using Drift.Networking.Grpc.Generated;
using Grpc.Net.Client;

namespace Drift.Networking.PeerStreaming.Core;

public interface IPeerClientFactory {
  (PeerService.PeerServiceClient Client, GrpcChannel Channel) Create( Uri address );
}