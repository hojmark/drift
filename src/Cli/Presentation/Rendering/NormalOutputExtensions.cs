using Drift.Cli.Presentation.Console.Managers.Abstractions;
using Spectre.Console;

namespace Drift.Cli.Presentation.Rendering;

internal static class NormalOutputExtensions {
  private const bool Emoji = true;

  /// <summary>
  /// Action here meaning a command.
  /// </summary>
  internal static void WriteLineCTA( this INormalOutput output, string description, string command ) {
    output.GetAnsiConsole().MarkupLine( $"{Chars.Bulb} {description} [bold][green]{command}[/][/]" );
  }

  internal static void WriteLineSuccess( this INormalOutput output, string message ) {
    output.GetAnsiConsole().MarkupLine( Emoji ? $"[green]{Chars.Checkmark}[/] {message}" : $"[green]{message}[/]" );
  }

  internal static void WriteLineFailure( this INormalOutput output, string message ) {
    output.GetAnsiConsole().MarkupLine( Emoji ? $"[red]{Chars.Cross}[/] {message}" : $"[red]{message}[/]" );
  }

  internal static void WriteAnsiMarkup( this INormalOutput output, string message ) {
    output.GetAnsiConsole().MarkupLine( message );
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