using System.CommandLine;
using Drift.Agent.Hosting;
using Drift.Cli.Abstractions;
using Drift.Cli.Commands.Agent.Subcommands.Start.Peers.Messages.Subnets;
using Drift.Cli.Commands.Agent.Subcommands.Start.Peers.Messages.Subnets.Request;
using Drift.Cli.Commands.Common;
using Drift.Cli.Infrastructure;
using Drift.Cli.Presentation.Console.Logging;
using Drift.Cli.Presentation.Console.Managers.Abstractions;
using Drift.Cli.SpecFile;
using Drift.Domain;
using Drift.Networking.Cluster;
using Drift.Networking.PeerStreaming.Messages;

namespace Drift.Cli.Commands.Agent.Subcommands.Start;

internal class AgentStartCommand : CommandBase<AgentStartParameters, AgentStartCommandHandler> {
  internal AgentStartCommand( IServiceProvider provider )
    : base( "start", "Start a local Drift agent process", provider ) {
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
  ISpecFileProvider specProvider
) : ICommandHandler<AgentStartParameters> {
  public async Task<int> Invoke( AgentStartParameters parameters, CancellationToken cancellationToken ) {
    output.Log.LogDebug( "Running 'agent start' command" );
    var logger = output.GetLogger();

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

    var configureServices = ( IServiceCollection services ) => {
      RootCommandFactory.ConfigureSubnetProvider( services );
      services.AddScoped<IPeerMessageHandler, GiveMeSubnetsRequestHandler>();
    };

    await AgentHost.Run( parameters.Port, logger, configureServices, cancellationToken );

    output.Log.LogDebug( "Completed 'agent start' command" );

    return ExitCodes.Success;
  }


  private AgentId? LoadAgentIdentity() {
    if ( false ) {
      return AgentId.New(); // TODO load from file
    }

    return null;
  }
}