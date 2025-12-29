using System.CommandLine;
using Drift.Agent.Hosting;
using Drift.Agent.PeerProtocol;
using Drift.Cli.Abstractions;
using Drift.Cli.Commands.Common.Commands;
using Drift.Cli.Infrastructure;
using Drift.Cli.Presentation.Console.Logging;
using Drift.Cli.Presentation.Console.Managers.Abstractions;
using Drift.Domain;
using Drift.Networking.Cluster;

namespace Drift.Cli.Commands.Agent.Subcommands.Start;

internal class AgentStartCommand : CommandBase<AgentStartParameters, AgentStartCommandHandler> {
  internal AgentStartCommand( IServiceProvider provider ) : base( "start", "Start a local Drift agent", provider ) {
    Options.Add( AgentStartParameters.Options.Port );
    Options.Add( AgentStartParameters.Options.Adoptable );
    Options.Add( AgentStartParameters.Options.Join );
  }

  protected override AgentStartParameters CreateParameters( ParseResult result ) {
    return new AgentStartParameters( result );
  }
}

internal class AgentStartCommandHandler(
  IOutputManager output,
  AgentLifetime? agentLifetime = null,
  Action<IServiceCollection>? configureServicesOverride = null
)
  : ICommandHandler<AgentStartParameters> {
  public async Task<int> Invoke( AgentStartParameters parameters, CancellationToken cancellationToken ) {
    output.Log.LogDebug( "Running 'agent start' command" );

    var logger = output.GetLogger();

    logger.LogInformation( "Agent starting.." );

    var identity = LoadAgentIdentity();

    if ( identity == null ) {
      logger.LogDebug( "Agent is not enrolled" );

      var enrollmentRequest = new EnrollmentRequest( parameters.Adoptable, parameters.Join );
      logger.LogInformation( "Agent cluster enrollment method is {EnrollmentMethod}", enrollmentRequest.Method );
    }
    else {
      logger.LogDebug( "Agent is enrolled into cluster 'milkyway'" );
      logger.LogInformation( "Attempting to re-join cluster 'milkyway'..." );
    }

    /*Inventory? inventory;

    try {
      inventory = await specProvider.GetDeserializedAsync( parameters.SpecFile );
    }
    catch ( FileNotFoundException ) {
      return ExitCodes.GeneralError;
    }*/

    output.Log.LogDebug( "Starting agent..." );

    await AgentHost.Run( parameters.Port, logger, ConfigureServices, cancellationToken, agentLifetime?.Ready );

    output.Log.LogDebug( "Completed 'agent start' command" );

    return ExitCodes.Success;

    void ConfigureServices( IServiceCollection services ) {
      RootCommandFactory.ConfigureSubnetProvider( services );
      services.AddPeerProtocol();
      configureServicesOverride?.Invoke( services );
    }
  }

  private static AgentId? LoadAgentIdentity() {
    if ( false ) {
      return AgentId.New(); // TODO load from file
    }

    return null;
  }
}