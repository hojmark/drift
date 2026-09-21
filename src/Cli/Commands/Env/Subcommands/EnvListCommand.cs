using System.CommandLine;
using Drift.Cli.Abstractions;
using Drift.Cli.Commands.Common.Commands;
using Drift.Cli.Commands.Common.Parameters;
using Drift.Cli.Presentation.Console.Logging;
using Drift.Cli.Presentation.Console.Managers.Abstractions;
using Drift.Cli.Settings.Serialization;
using Drift.Cli.Settings.V1_preview;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace Drift.Cli.Commands.Env.Subcommands;

internal class EnvListCommand : CommandBase<EnvListParameters, EnvListCommandHandler> {
  internal EnvListCommand( IServiceProvider provider ) : base(
    "list",
    "List Drift environments",
    provider,
    includeSpecArgument: false
  ) {
    Aliases.Add( "ls" );
  }

  protected override EnvListParameters CreateParameters( ParseResult result ) {
    return new EnvListParameters( result );
  }
}

internal record EnvListParameters : BaseParameters {
  internal EnvListParameters( ParseResult parseResult ) : base( parseResult ) {
  }
}

internal class EnvListCommandHandler(
  IOutputManager output,
  ISettingsLocation settingsLocation
) : ICommandHandler<EnvListParameters> {
  public Task<int> Invoke( EnvListParameters parameters, CancellationToken cancellationToken ) {
    output.Log.LogDebug( "Running 'env list' command" );

    var settings = CliSettings.Read( settingsLocation, output.GetLogger() );

    var table = new Table { Border = new NoTableBorder(), ShowHeaders = false };
    table.AddColumn( new TableColumn( "Status" ) { Width = 1 } );
    table.AddColumn( new TableColumn( "Name" ) );
    table.AddColumn( new TableColumn( "Address" ) );

    var localIsActive = string.IsNullOrWhiteSpace( settings.ActiveEnvironment );
    table.AddRow(
      localIsActive ? "[bold][green]*[/][/]" : " ",
      localIsActive ? $"[bold]{BuiltInEnvironmentNames.Local}[/]" : BuiltInEnvironmentNames.Local,
      string.Empty
    );

    foreach ( var environment in settings.Environments ) {
      var isActive = environment.Name == settings.ActiveEnvironment;

      table.AddRow(
        isActive ? "[bold][green]*[/][/]" : " ",
        isActive ? "[bold]" + environment.Name + "[/]" : environment.Name,
        isActive ? "[bold]" + "@ " + environment.Address + "[/]" : "@ " + environment.Address
      );
    }

    output.Normal.GetAnsiConsole().Write( table );

    return Task.FromResult( ExitCodes.Success );
  }
}