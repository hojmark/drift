using System.CommandLine;
using Drift.Agent.Host;
using Drift.Cli.Abstractions;
using Drift.Cli.Commands.Agent.Subcommands;
using Drift.Cli.Commands.Common.Commands;
using Drift.Cli.Infrastructure;
using Drift.Cli.Presentation.Console.Logging;
using Drift.Cli.Presentation.Console.Managers.Abstractions;
using Drift.Coordinator.Host;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Drift.Cli.Commands.Server.Subcommands.Start;

// TODO No spec should be provided - do not inherit from CommandBase - or CommandBase should not include it
internal class ServerStartCommand : CommandBase<ServerStartParameters, ServerStartCommandHandler> {
  internal ServerStartCommand( IServiceProvider provider ) : base( "start", "Start a local Drift server", provider ) {
    Options.Add( ServerStartParameters.Options.PortS );
    Options.Add( ServerStartParameters.Options.PortAgent );
  }

  protected override ServerStartParameters CreateParameters( ParseResult result ) {
    return new ServerStartParameters( result );
  }
}

internal class ServerStartCommandHandler(
  IOutputManager output,
  AgentLifetime? agentLifetime = null,
  Action<IServiceCollection>? configureServicesOverride = null
)
  : ICommandHandler<ServerStartParameters> {
  public async Task<int> Invoke( ServerStartParameters parameters, CancellationToken cancellationToken ) {
    output.Log.LogDebug( "Running 'server start' command" );

    output.WarnAgentPreview();

    var logger = output.GetLogger();

    logger.LogInformation( "Server starting..." );

    /*Inventory? inventory;

    try {
      inventory = await specProvider.GetDeserializedAsync( parameters.SpecFile );
    }
    catch ( FileNotFoundException ) {
      return ExitCodes.GeneralError;
    }*/

    output.Log.LogDebug( "Starting server..." );

    try {
      await CoordinatorHost.Run(
        parameters.PortS,
        logger,
        ConfigureServices,
        cancellationToken,
        agentLifetime?.Ready
      );
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