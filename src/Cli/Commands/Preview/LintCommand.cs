using System.CommandLine;
using Drift.Cli.Abstractions;
using Drift.Cli.Commands.Global;
using Drift.Cli.Output;
using Drift.Cli.Output.Abstractions;
using Microsoft.Extensions.Logging;

namespace Drift.Cli.Commands.Preview;

internal class LintCommand : Command {
  internal LintCommand( ILoggerFactory loggerFactory ) : base( "lint", "Validate network spec" ) {
    AddOption( GlobalParameters.Options.Verbose );

    AddOption( GlobalParameters.Options.OutputFormatOption );

    AddArgument( GlobalParameters.Arguments.SpecOptional );

    this.SetHandler(
      CommandHandler,
      new ConsoleOutputManagerBinder( loggerFactory ),
      GlobalParameters.Arguments.SpecOptional,
      GlobalParameters.Options.OutputFormatOption
    );
  }

  private static async Task<int> CommandHandler(
    IOutputManager output,
    FileInfo? specFile,
    GlobalParameters.OutputFormat outputFormat
  ) {
    output.Log.LogDebug( "Running lint command" );
    
    // TODO implement

    return ExitCodes.Success;
  }
}