using System.CommandLine;
using Drift.Agent.Host;
using Drift.Cli.Abstractions;
using Drift.Cli.Commands.Common.Commands;
using Drift.Cli.Infrastructure;
using Drift.Cli.Presentation.Console.Logging;
using Drift.Cli.Presentation.Console.Managers.Abstractions;
using Drift.Common.IO;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

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
  IDriftDataLocation driftDataLocation,
  IDriftSettingsLocation driftSettingsLocation,
  NestedHostLifetime? agentLifetime = null,
  AgentHostServiceConfiguration? hostServiceConfiguration = null
)
  : ICommandHandler<AgentStartParameters> {
  public async Task<int> Invoke( AgentStartParameters parameters, CancellationToken cancellationToken ) {
    output.Log.LogDebug( "Running 'agent start' command" );

    output.WarnAgentPreview();

    var logger = output.GetLogger();

    logger.LogDebug( "Agent starting..." );

    /*Inventory? inventory;

    try {
      inventory = await specProvider.GetDeserializedAsync( parameters.SpecFile );
    }
    catch ( FileNotFoundException ) {
      return ExitCodes.GeneralError;
    }*/

    try {
      var options = HostOptionsLoader.Load(
        driftSettingsLocation.AgentConfigFile,
        EnvVarPrefix.Agent,
        AgentOptions.Bind,
        logger
      );
      parameters.ApplyTo( options );

      await AgentHost.Run(
        options,
        logger,
        services => {
          // The host uses the same data location as the CLI
          services.Replace( ServiceDescriptor.Singleton( driftDataLocation ) );
          hostServiceConfiguration?.Configure( services );
        },
        cancellationToken,
        agentLifetime?.Ready
      );
    }
    catch ( HostConfigurationException e ) {
      // The message is actionable on its own; a stack trace would only obscure a configuration mistake
#pragma warning disable S6667
      logger.LogError( "{Message}", e.Message );
#pragma warning restore S6667
      return ExitCodes.GeneralError;
    }
    catch ( OptionsValidationException e ) {
#pragma warning disable S6667
      logger.LogError( "Invalid agent configuration: {Message}", e.Message );
#pragma warning restore S6667
      return ExitCodes.GeneralError;
    }
    catch ( OperationCanceledException ) when ( cancellationToken.IsCancellationRequested ) {
      // Graceful shutdown via cancellation
    }

    output.Log.LogDebug( "Completed 'agent start' command" );

    return ExitCodes.Success;
  }
}