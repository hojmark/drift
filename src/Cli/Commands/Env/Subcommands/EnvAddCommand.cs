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

internal class EnvAddCommand : CommandBase<EnvAddParameters, EnvAddCommandHandler> {
  internal EnvAddCommand( IServiceProvider provider ) : base(
    "add",
    "Add a new Drift environment",
    provider,
    includeSpecArgument: false
  ) {
    Arguments.Add( EnvAddParameters.Arguments.Name );
    Arguments.Add( EnvAddParameters.Arguments.Uri );
  }

  protected override EnvAddParameters CreateParameters( ParseResult result ) {
    return new EnvAddParameters( result );
  }
}

internal record EnvAddParameters : BaseParameters {
  internal static class Arguments {
    internal static readonly Argument<string> Name = new("name") { Description = "The environment name" };

    internal static readonly Argument<string> Uri = new("uri") { Description = "The agent address, e.g. host:port" };
  }

  internal EnvAddParameters( ParseResult parseResult ) : base( parseResult ) {
    Name = parseResult.GetValue( Arguments.Name )!;
    Address = parseResult.GetValue( Arguments.Uri )!;
  }

  internal string Name {
    get;
  }

  internal string Address {
    get;
  }
}

internal class EnvAddCommandHandler(
  IOutputManager output,
  ISettingsLocationProvider settingsLocation
) : ICommandHandler<EnvAddParameters> {
  public Task<int> Invoke( EnvAddParameters parameters, CancellationToken cancellationToken ) {
    output.Log.LogDebug( "Running 'env add' command" );

    var settings = CliSettings.Read( settingsLocation, output.GetLogger() );

    if ( settings.TryGetEnvironment( parameters.Name, out _ ) ) {
      output.Normal.WriteLineFailure( $"'{parameters.Name}' already exist" );
      return Task.FromResult( ExitCodes.GeneralError );
    }

    settings.Environments.Add( new EnvironmentSetting( parameters.Name, parameters.Address ) );

    if ( settings.Environments.Count == 1 ) {
      settings.ActiveEnvironment = parameters.Name;
    }

    settings.Write( output.GetLogger(), settingsLocation );

    output.Normal.WriteLineSuccess( $"Added '{parameters.Name}@{parameters.Address}'" );

    return Task.FromResult( ExitCodes.Success );
  }
}