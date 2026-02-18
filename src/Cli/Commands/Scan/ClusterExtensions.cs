using Drift.Agent.PeerProtocol.Scan;
using Drift.Agent.PeerProtocol.Subnets;
using Drift.Domain;
using Drift.Networking.Cluster;
using Drift.Networking.Grpc.Generated;
using Drift.Networking.PeerStreaming.Core.Abstractions;

namespace Drift.Cli.Commands.Scan;

internal static class ClusterExtensions {
  internal static Task<SubnetsResponse> GetSubnetsAsync(
    this ICluster cluster,
    Domain.Agent agent,
    CancellationToken cancellationToken
  ) {
    return cluster.SendAndWaitAsync<SubnetsRequest, SubnetsResponse>(
      agent,
      new SubnetsRequest(),
      timeout: TimeSpan.FromSeconds( 10 ),
      cancellationToken
    );
  }

  internal static Task<ScanSubnetCompleteResponse> ScanSubnetAsync(
    this ICluster cluster,
    Domain.Agent agent,
    CidrBlock cidr,
    IPeerMessageEnvelopeConverter converter,
    Action<ScanSubnetProgressUpdate> onProgressUpdate,
    CancellationToken cancellationToken
  ) {
    var request = new ScanSubnetRequest { Cidr = cidr, PingsPerSecond = 1000 };

    return cluster.SendAndWaitStreamingAsync<ScanSubnetRequest, ScanSubnetCompleteResponse>(
      agent,
      request,
      ScanSubnetCompleteResponse.MessageType,
      ( progressEnvelope ) => {
        // Deserialize progress update and call handler
        if ( progressEnvelope.MessageType == ScanSubnetProgressUpdate.MessageType ) {
          var progressUpdate = converter.FromEnvelope<ScanSubnetProgressUpdate>( progressEnvelope );
          onProgressUpdate( progressUpdate );
        }
      },
      timeout: TimeSpan.FromMinutes( 10 ),
      cancellationToken
    );
  }
}