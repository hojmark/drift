using Drift.Agent.PeerProtocol.Subnets;
using Drift.Networking.Clustering;

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
}