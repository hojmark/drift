using Drift.Cli.Commands.Agent.Subcommands.Start.Peers.Messages.Subnets.Request;
using Drift.Cli.Commands.Agent.Subcommands.Start.Peers.Messages.Subnets.Response;
using Drift.Networking.Clustering;

namespace Drift.Cli.Commands.Scan;

internal static class ClusterExtensions {
  internal static Task<SubnetsResponse> GetSubnetsAsync(
    this ICluster cluster,
    Domain.Agent agent,
    CancellationToken cancellationToken
  ) {
    return cluster.SendAndWaitAsync<SubnetsResponse>(
      agent,
      new SubnetsRequest(),
      timeout: TimeSpan.FromSeconds( 10 ),
      cancellationToken
    );
  }
}