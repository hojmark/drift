using System.CommandLine;
using Drift.Agent.Host;
using Drift.Cli.Abstractions;
using Drift.Cli.Commands.Common.Commands;
using Drift.Cli.Infrastructure;
using Drift.Cli.Presentation.Console.Logging;
using Drift.Cli.Presentation.Console.Managers.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Drift.Cli.Commands.Agent.Subcommands.Start;

// TODO No spec should be provided - do not inherit from CommandBase - or CommandBase should not include it
internal class AgentStartCommand : CommandBase<AgentStartParameters, AgentStartCommandHandler> {
  internal AgentStartCommand( IServiceProvider provider ) : base( "start", "Start a local Drift agent", provider ) {
    Options.Add( AgentStartParameters.Options.Port );
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

    /*Inventory? inventory;

    try {
      inventory = await specProvider.GetDeserializedAsync( parameters.SpecFile );
    }
    catch ( FileNotFoundException ) {
      return ExitCodes.GeneralError;
    }*/

    output.Log.LogDebug( "Starting agent..." );

    try {
      await AgentHost.Run( parameters.Port, logger, ConfigureServices, cancellationToken, agentLifetime?.Ready );
    }
    catch ( OperationCanceledException ) when ( cancellationToken.IsCancellationRequested ) {
      // Graceful shutdown via cancellation
    }

    output.Log.LogDebug( "Completed 'agent start' command" );

    return ExitCodes.Success;

    void ConfigureServices( IServiceCollection services ) {
      // Configure core agent services (scanning, subnet discovery, execution environment)
      RootCommandFactory.ConfigureAgentCoreServices( services );

      // Add peer protocol message handlers
      services.AddAgentHandlers();

      // Allow test overrides
      configureServicesOverride?.Invoke( services );
    }
  }
}