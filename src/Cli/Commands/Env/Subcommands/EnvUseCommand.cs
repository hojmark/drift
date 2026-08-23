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

internal class EnvUseCommand : CommandBase<EnvUseParameters, EnvUseCommandHandler> {
  internal EnvUseCommand( IServiceProvider provider ) : base(
    "use",
    "Switch the active Drift environment",
    provider,
    includeSpecArgument: false
  ) {
    Arguments.Add( EnvUseParameters.Arguments.Name );
  }

  protected override EnvUseParameters CreateParameters( ParseResult result ) {
    return new EnvUseParameters( result );
  }
}

internal record EnvUseParameters : BaseParameters {
  internal static class Arguments {
    internal static readonly Argument<string> Name = new("name") { Description = "The environment name" };
  }

  internal EnvUseParameters( ParseResult parseResult ) : base( parseResult ) {
    Name = parseResult.GetValue( Arguments.Name )!;
  }

  internal string Name {
    get;
  }
}

internal class EnvUseCommandHandler(
  IOutputManager output,
  ISettingsLocationProvider settingsLocation
) : ICommandHandler<EnvUseParameters> {
  public Task<int> Invoke( EnvUseParameters parameters, CancellationToken cancellationToken ) {
    output.Log.LogDebug( "Running 'env use' command" );

    var settings = CliSettings.Read( output.GetLogger(), settingsLocation );

    if ( !settings.TryGetEnvironment( parameters.Name, out _ ) ) {
      output.Normal.WriteLineFailure( $"'{parameters.Name}' does not exist" );
      return Task.FromResult( ExitCodes.GeneralError );
    }

    settings.ActiveEnvironment = parameters.Name;
    settings.Write( output.GetLogger(), settingsLocation );

    output.Normal.WriteLineSuccess( $"'{parameters.Name}' is active" );

    return Task.FromResult( ExitCodes.Success );
  }
}
