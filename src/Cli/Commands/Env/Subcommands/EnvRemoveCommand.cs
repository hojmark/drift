using System.CommandLine;
using Drift.Cli.Abstractions;
using Drift.Cli.Commands.Common.Commands;
using Drift.Cli.Commands.Common.Parameters;
using Drift.Cli.Presentation.Console.Logging;
using Drift.Cli.Presentation.Console.Managers.Abstractions;
using Drift.Cli.Presentation.Rendering;
using Drift.Cli.Settings.Serialization;
using Drift.Cli.Settings.V1_preview;
using Drift.Cli.Settings.V1_preview.Environments;
using Microsoft.Extensions.Logging;

namespace Drift.Cli.Commands.Env.Subcommands;

internal class EnvRemoveCommand : CommandBase<EnvRemoveParameters, EnvRemoveCommandHandler> {
  internal EnvRemoveCommand( IServiceProvider provider ) : base(
    "remove",
    "Remove a Drift environment",
    provider,
    includeSpecArgument: false
  ) {
    Arguments.Add( EnvRemoveParameters.Arguments.Name );
  }

  protected override EnvRemoveParameters CreateParameters( ParseResult result ) {
    return new EnvRemoveParameters( result );
  }
}

internal record EnvRemoveParameters : BaseParameters {
  internal static class Arguments {
    internal static readonly Argument<string> Name = new("name") { Description = "The environment name" };
  }

  internal EnvRemoveParameters( ParseResult parseResult ) : base( parseResult ) {
    Name = parseResult.GetValue( Arguments.Name )!;
  }

  internal string Name {
    get;
  }
}

internal class EnvRemoveCommandHandler(
  IOutputManager output,
  ISettingsLocationProvider settingsLocation
) : ICommandHandler<EnvRemoveParameters> {
  public Task<int> Invoke( EnvRemoveParameters parameters, CancellationToken cancellationToken ) {
    output.Log.LogDebug( "Running 'env remove' command" );

    var settings = CliSettings.Read( output.GetLogger(), settingsLocation );

    if ( !settings.TryGetEnvironment( parameters.Name, out _ ) ) {
      output.Normal.WriteLineFailure( $"'{parameters.Name}' does not exist" );
      return Task.FromResult( ExitCodes.GeneralError );
    }

    var removedActiveEnvironment = settings.ActiveEnvironment == parameters.Name;
    settings.Environments.RemoveAll( e => e.Name == parameters.Name );

    if ( removedActiveEnvironment ) {
      settings.ActiveEnvironment = null;
    }

    settings.Write( output.GetLogger(), settingsLocation );

    output.Normal.WriteLineSuccess( $"Removed '{parameters.Name}'" );

    if ( removedActiveEnvironment ) {
      if ( settings.Environments.Count > 0 ) {
        output.Normal.WriteLineWarning( "No environment is active." );
        output.Normal.WriteLineCTA( "Set one with", "drift env use <name>" );
      }
      else {
        output.Normal.WriteLineWarning( "No environments are configured." );
        output.Normal.WriteLineCTA( "Add one with", "drift env add <name> <address>" );
      }
    }

    return Task.FromResult( ExitCodes.Success );
  }
}