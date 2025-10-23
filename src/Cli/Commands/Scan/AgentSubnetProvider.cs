using Drift.Cli.Commands.Agent.Subcommands.Start.Peers.Messages.Subnets;
using Drift.Domain;
using Drift.Networking.Cluster;
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
    logger.LogInformation( "Getting subnets from agents" );

    foreach ( var agent in agents ) {
      logger.LogInformation( "Getting subnets from agent {Address}", agent.Address );

      try {
        await cluster.SendAsync( agent, new GiveMeSubnets(), CancellationToken.None );
      }
      catch ( Exception ex ) {
        logger.LogWarning( ex, "cluster.SendAsync failed", agent.Address );
      }

      logger.LogInformation( "WAIT" );
      await Task.Delay( 1000000, cancellationToken );
    }

    throw new NotImplementedException();
  }
}