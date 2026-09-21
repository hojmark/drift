using Drift.Networking.Core.Abstractions;
using Drift.Networking.Grpc.Generated;
using Grpc.Net.Client;

namespace Drift.Networking.Client;

internal sealed class DefaultMessagingClientFactory : IMessagingClientFactory {
  public (MessagingService.MessagingServiceClient Client, GrpcChannel Channel) Create( Uri address ) {
    var channel = GrpcChannel.ForAddress( address, new GrpcChannelOptions() );
    var client = new MessagingService.MessagingServiceClient( channel );
    return ( client, channel );
  }
}