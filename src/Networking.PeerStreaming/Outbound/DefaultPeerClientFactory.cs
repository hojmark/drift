using Drift.Networking.Grpc.Generated;
using Grpc.Net.Client;

namespace Drift.Networking.PeerStreaming.Outbound;

internal sealed class DefaultPeerClientFactory : IPeerClientFactory {
  public (PeerService.PeerServiceClient Client, GrpcChannel Channel) Create( Uri address ) {
    var channel = GrpcChannel.ForAddress( address, new GrpcChannelOptions() { } );
    var client = new PeerService.PeerServiceClient( channel );
    return ( client, channel );
  }
}