using Drift.Cli.Presentation.Output.Abstractions;
using Spectre.Console;

namespace Drift.Cli.Presentation.Rendering;

internal static class NormalOutputExtensions {
  
  internal static void WriteLineValidity( this INormalOutput output, bool isValid ) {
    if ( isValid ) {
      output.WriteLine( $"{Chars.Checkmark} Valid", ConsoleColor.Green );
    }
    else {
      output.WriteLineError( $"{Chars.Cross} Validation failed" );
    }
  }

  /// <summary>
  /// Action here meaning a command.
  /// </summary>
  internal static void WriteLineCTA( this INormalOutput output, string description, string command ) {
    output.GetAnsiConsole().MarkupLine( $"{description} [bold][green]{command}[/][/]" );
  }
/*
  /// <summary>
  /// Writes a formatted line to the output, interpolating values into the template (using {0}, {1}, ...).
  /// </summary>
  internal static void WriteLineWithVariable( this INormalOutput output, string template, params string[] values ) {
    var formattedText = string.Format( template, values );
    output.WriteLine( formattedText );
  }*/
}