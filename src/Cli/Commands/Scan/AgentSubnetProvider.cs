using Drift.Cli.Commands.Agent.Subcommands.Start.Peers.Messages.Subnets.Request;
using Drift.Cli.Commands.Agent.Subcommands.Start.Peers.Messages.Subnets.Response;
using Drift.Domain;
using Drift.Networking.Clustering;
using Drift.Scanning.Subnets;

namespace Drift.Cli.Commands.Scan;

public class AgentSubnetProvider(
  ILogger logger,
  List<Domain.Agent> agents,
  ICluster cluster,
  CancellationToken cancellationToken
)
  : ISubnetProvider {
  public async Task<List<CidrBlock>> GetAsync() {
    logger.LogDebug( "Getting subnets from agents" );
    var allSubnets = new List<CidrBlock>();

    foreach ( var agent in agents ) {
      logger.LogInformation( "Requesting subnets from agent {Id} ({Address})", agent.Id, agent.Address );

      try {
        // await cluster.SendAsync( agent, new GiveMeSubnetsRequest(), CancellationToken.None );

        var response = await cluster.SendAndWaitAsync<GiveMeSubnetsResponse>(
          agent,
          new GiveMeSubnetsRequest(),
          timeout: TimeSpan.FromSeconds( 10 ),
          cancellationToken
        );

        allSubnets.AddRange( response.Subnets );
        logger.LogInformation(
          "Received subnet(s) from agent {Id} ({Address}): {Subnets}",
          response.Subnets.Count,
          agent.Address,
          string.Join( ", ", response.Subnets )
        );
      }
      catch ( Exception ex ) {
        logger.LogWarning( ex, "cluster.SendAsync failed", agent.Address );
      }
    }

    return allSubnets;
  }
}