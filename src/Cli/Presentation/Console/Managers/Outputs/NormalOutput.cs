using System.Diagnostics.CodeAnalysis;
using Drift.Cli.Presentation.Console.Managers.Abstractions;
using Spectre.Console;

namespace Drift.Cli.Presentation.Console.Managers.Outputs;

[SuppressMessage(
  "ApiDesign",
  "RS0030:Do not use banned APIs",
  Justification = "Main point is to centralize usage of the banned APIs to this class"
)]
internal partial class NormalOutput(
  TextWriter stdOut,
  TextWriter errOut,
  bool plainOutput = false,
  bool verbose = false,
  bool veryVerbose = false
) : INormalOutput {
  // TODO test
  private const bool MarkupOutput = false;

  public IAnsiConsole GetAnsiConsole() {
    var settings = new AnsiConsoleSettings { Out = new AnsiConsoleOutput( stdOut ) };

    if ( plainOutput ) {
      settings.Ansi = AnsiSupport.No;
      settings.ColorSystem = ColorSystemSupport.NoColors;
      settings.Enrichment = new ProfileEnrichment { UseDefaultEnrichers = false };
    }

    var customAnsiConsole = AnsiConsole.Create( settings );
    return customAnsiConsole;
  }

  private static void WriteInternal(
    TextWriter textWriter,
    int level,
    string? text = null,
    ConsoleColor? foreground = null,
    ConsoleColor? background = null
  ) {
    textWriter.Write( new string( ' ', level * 2 ) );

    if ( foreground.HasValue ) {
      System.Console.ForegroundColor = foreground.Value;
    }

    if ( background.HasValue ) {
      System.Console.BackgroundColor = background.Value;
    }

    textWriter.Write( text );

    System.Console.ResetColor();
  }

#pragma warning disable CS0162 // Unreachable code detected
  private static void WriteLineInternal(
    TextWriter textWriter,
    int level,
    string text,
    ConsoleColor? foreground = null,
    ConsoleColor? background = null
  ) {
    var lines = text.Split( '\n' );
    if ( lines[^1] == string.Empty ) {
      lines = lines.Take( lines.Length - 1 ).ToArray();
    }

    foreach ( var line in lines ) {
      textWriter.Write( new string( ' ', level * 2 ) );

      if ( MarkupOutput ) {
        if ( foreground.HasValue ) {
          textWriter.Write( $"[{foreground.Value}]" );
        }

        if ( background.HasValue ) {
          // ... on bgcolor]
        }
      }

      if ( foreground.HasValue ) {
        System.Console.ForegroundColor = foreground.Value;
      }

      if ( background.HasValue ) {
        System.Console.BackgroundColor = background.Value;
      }

      textWriter.Write( line );

      if ( MarkupOutput ) {
        if ( foreground.HasValue ) {
          textWriter.Write( "[/]" );
        }

        if ( background.HasValue ) {
          System.Console.BackgroundColor = background.Value;
        }
      }

      System.Console.ResetColor();

      textWriter.WriteLine();
    }
  }
}
#pragma warning restore CS0162 // Unreachable code detected