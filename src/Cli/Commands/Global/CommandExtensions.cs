using System.CommandLine;

namespace Drift.Cli.Commands.Global;

internal static class CommandExtensions {
  //TODO not working... find another solution
  internal static Command WithExamples( this Command command, params string[] examples ) {
    if ( command.Description != null && command.Description.Contains( "\n\n" ) )
      throw new ArgumentException(
        @"Command.Description contains a paragraph break (\n\n), which interferes with WithExamples formatting." );
    if ( examples.Length == 0 )
      return command;

    var examplesBlock = "Examples:\n  " + string.Join( "\n  ", examples.Select( e => e.Trim() ) );

    command.Description = string.IsNullOrWhiteSpace( command.Description )
      ? examplesBlock
      : $"{command.Description.Trim()}\n\n{examplesBlock}";

    return command;
  }
}