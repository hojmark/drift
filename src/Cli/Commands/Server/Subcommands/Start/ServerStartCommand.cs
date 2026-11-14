using System.CommandLine;
using Drift.Cli.Abstractions;
using Drift.Cli.Commands.Common.Commands;
using Drift.Cli.Infrastructure;
using Drift.Cli.Presentation.Console.Logging;
using Drift.Cli.Presentation.Console.Managers.Abstractions;
using Drift.Common.IO;
using Drift.Coordinator.Host;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Drift.Cli.Commands.Server.Subcommands.Start;

// TODO No spec should be provided - do not inherit from CommandBase - or CommandBase should not include it
internal class ServerStartCommand : CommandBase<ServerStartParameters, ServerStartCommandHandler> {
  internal ServerStartCommand( IServiceProvider provider ) : base( "start", "Start a local Drift server", provider ) {
    Options.Add( ServerStartParameters.Options.PortClient );
    Options.Add( ServerStartParameters.Options.PortAgent );
    Options.Add( ServerStartParameters.Options.NoAgentPort );
  }

  protected override ServerStartParameters CreateParameters( ParseResult result ) {
    return new ServerStartParameters( result );
  }
}

internal class ServerStartCommandHandler(
  IOutputManager output,
  IDriftDataLocation driftDataLocation,
  IDriftSettingsLocation driftSettingsLocation,
  NestedHostLifetime? coordinatorLifetime = null,
  CoordinatorHostServiceConfiguration? hostServiceConfiguration = null
)
  : ICommandHandler<ServerStartParameters> {
  public async Task<int> Invoke( ServerStartParameters parameters, CancellationToken cancellationToken ) {
    output.Log.LogDebug( "Running 'server start' command" );

    output.WarnAgentPreview();

    var logger = output.GetLogger();

    logger.LogDebug( "Server starting..." );

    /*Inventory? inventory;

    try {
      inventory = await specProvider.GetDeserializedAsync( parameters.SpecFile );
    }
    catch ( FileNotFoundException ) {
      return ExitCodes.GeneralError;
    }*/

    try {
      var options = HostOptionsLoader.Load(
        driftSettingsLocation.ServerConfigFile,
        EnvVarPrefix.Server,
        CoordinatorOptions.Bind,
        logger
      );
      parameters.ApplyTo( options );

      await CoordinatorHost.Run(
        options,
        logger,
        services => {
          // The host uses the same data location as the CLI
          services.Replace( ServiceDescriptor.Singleton( driftDataLocation ) );
          hostServiceConfiguration?.Configure( services );
        },
        cancellationToken,
        coordinatorLifetime?.Ready
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
      logger.LogError( "Invalid server configuration: {Message}", e.Message );
#pragma warning restore S6667
      return ExitCodes.GeneralError;
    }
    catch ( OperationCanceledException ) when ( cancellationToken.IsCancellationRequested ) {
      // Graceful shutdown via cancellation
    }

    output.Log.LogDebug( "Completed 'server start' command" );

    return ExitCodes.Success;
  }
}