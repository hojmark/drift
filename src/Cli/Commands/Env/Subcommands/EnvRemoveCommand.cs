using System.CommandLine;
using Drift.Cli.Abstractions;
using Drift.Cli.Commands.Common.Commands;
using Drift.Cli.Commands.Common.Parameters;
using Drift.Cli.Presentation.Console.Logging;
using Drift.Cli.Presentation.Console.Managers.Abstractions;
using Drift.Cli.Presentation.Rendering;
using Drift.Cli.Settings.V1_preview;
using Drift.Cli.Settings.V1_preview.Environments;
using Drift.Common.IO;
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
  IDriftSettingsLocation settingsLocation
) : ICommandHandler<EnvRemoveParameters> {
  public Task<int> Invoke( EnvRemoveParameters parameters, CancellationToken cancellationToken ) {
    output.Log.LogDebug( "Running 'env remove' command" );

    if ( string.Equals( parameters.Name, BuiltInEnvironmentNames.Local, StringComparison.OrdinalIgnoreCase ) ) {
      output.Normal.WriteLineFailure( "The built-in local environment cannot be removed." );
      return Task.FromResult( ExitCodes.GeneralError );
    }

    var settings = CliSettings.Read( settingsLocation, output.GetLogger() );

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
      output.Normal.WriteLine( $"'{BuiltInEnvironmentNames.Local}' is active" );
    }

    return Task.FromResult( ExitCodes.Success );
  }
}