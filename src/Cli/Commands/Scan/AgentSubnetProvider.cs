using Drift.Cli.Commands.Preview.Agent.Subcommands.Peers.Messages;
using Drift.Cli.Commands.Preview.Agent.Subcommands.Utils;
using Drift.Domain;
using Drift.Networking.Grpc.Generated;
using Drift.Scanning.Subnets;

namespace Drift.Cli.Commands.Scan;

public class AgentSubnetProvider(
  ILogger logger,
  List<Agent> agents,
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