using Drift.Networking.Grpc.Generated;
using Grpc.Net.Client;

namespace Drift.Networking.Core.Abstractions;

public interface IMessagingClientFactory {
  (MessagingService.MessagingServiceClient Client, GrpcChannel Channel) Create( Uri address );
}