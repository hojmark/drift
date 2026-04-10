using System.CommandLine;
using Drift.Agent.Hosting;
using Drift.Agent.Hosting.Identity;
using Drift.Agent.PeerProtocol;
using Drift.Cli.Abstractions;
using Drift.Cli.Commands.Common.Commands;
using Drift.Cli.Infrastructure;
using Drift.Cli.Presentation.Console.Logging;
using Drift.Cli.Presentation.Console.Managers.Abstractions;
using Drift.Domain;
using Drift.Networking.Cluster;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Drift.Cli.Commands.Agent.Subcommands.Start;

internal class AgentStartCommand : CommandBase<AgentStartParameters, AgentStartCommandHandler> {
  internal AgentStartCommand( IServiceProvider provider ) : base( "start", "Start a local Drift agent", provider ) {
    Options.Add( AgentStartParameters.Options.Port );
    Options.Add( AgentStartParameters.Options.Adoptable );
    Options.Add( AgentStartParameters.Options.Join );
    Options.Add( AgentStartParameters.Options.Id );
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

    output.WarnAgentPreview();

    var logger = output.GetLogger();

    logger.LogInformation( "Agent starting..." );

    _ = LoadAgentIdentity( parameters.Id );

    // Check if agent has cluster membership info
    var agentIdentity = AgentIdentity.Load( logger );
    var isEnrolled = agentIdentity.ClusterId != null;

    if ( !isEnrolled ) {
      logger.LogDebug( "Agent is not enrolled" );

      var enrollmentRequest = new EnrollmentRequest( parameters.Adoptable, parameters.Join );
      logger.LogInformation( "Agent cluster enrollment method is {EnrollmentMethod}", enrollmentRequest.Method );
    }
    else {
      logger.LogDebug( "Agent is enrolled into cluster '{ClusterId}'", agentIdentity.ClusterId );
      logger.LogInformation( "Attempting to re-join cluster '{ClusterId}'...", agentIdentity.ClusterId );
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
      // Configure core agent services (scanning, subnet discovery, execution environment)
      RootCommandFactory.ConfigureAgentCoreServices( services );

      // Add peer protocol message handlers
      services.AddPeerProtocol();

      // Allow test overrides
      configureServicesOverride?.Invoke( services );
    }
  }

  private AgentId LoadAgentIdentity( string? idOverride ) {
    var logger = output.GetLogger();
    IAgentIdentityLocationProvider locationProvider = new DefaultAgentIdentityLocationProvider();
    var identityFilePath = locationProvider.GetFile();

    // If an ID override is provided, use it directly without loading/saving
    if ( !string.IsNullOrWhiteSpace( idOverride ) ) {
      logger.LogWarning( "Agent started with --id flag. This should only be used for testing purposes." );
      logger.LogInformation( "Using provided agent ID: {AgentId}", idOverride );
      return new AgentId( idOverride );
    }

    // Load existing identity or create new one
    var identity = AgentIdentity.Load( logger, locationProvider );

    // Save immediately if it's new to persist it
    if ( !File.Exists( identityFilePath ) ) {
      identity.Save( logger, locationProvider );
      logger.LogInformation( "Generated and saved new agent identity: {AgentId}", identity.Id );
    }
    else {
      logger.LogDebug( "Loaded existing agent identity: {AgentId}", identity.Id );
    }

    return identity.Id;
  }
}