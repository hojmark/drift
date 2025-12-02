using Drift.Domain;
using Drift.Networking.Cluster;
using Drift.Scanning.Subnets;

namespace Drift.Cli.Commands.Scan;

internal sealed class AgentSubnetProvider(
  ILogger logger,
  List<Domain.Agent> agents,
  ICluster cluster,
  CancellationToken cancellationToken
) : ISubnetProvider {
  public async Task<List<CidrBlock>> GetAsync() {
    logger.LogDebug( "Getting subnets from agents" );
    var allSubnets = new List<CidrBlock>();

    foreach ( var agent in agents ) {
      logger.LogInformation( "Requesting subnets from agent {Id} ({Address})", agent.Id, agent.Address );

      try {
        var response = await cluster.GetSubnetsAsync( agent, cancellationToken );

        logger.LogInformation(
          "Received subnet(s) from agent {Id} ({Address}): {Subnets}",
          agent.Id,
          agent.Address,
          string.Join( ", ", response.Subnets )
        );

        allSubnets.AddRange( response.Subnets );
      }
      catch ( Exception ex ) {
        logger.LogInformation( ex, "Failed requesting subnets from agent {Id} ({Address})", agent.Id, agent.Address );
      }
    }

    return allSubnets;
  }
}