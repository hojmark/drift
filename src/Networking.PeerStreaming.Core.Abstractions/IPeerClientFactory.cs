using Drift.Networking.Grpc.Generated;
using Grpc.Net.Client;

namespace Drift.Networking.PeerStreaming.Core.Abstractions;

public interface IPeerClientFactory {
  (PeerService.PeerServiceClient Client, GrpcChannel Channel) Create( Uri address );
}