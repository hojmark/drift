using System.Text.RegularExpressions;
using Drift.Cli.Presentation.Output.Abstractions;
using Drift.Cli.Presentation.Rendering;

namespace Drift.Cli.Presentation.Prompts;

internal static class NormalOutputExtensions {
  internal static string PromptString(
    this INormalOutput console,
    string question,
    string defaultValue,
    Regex? regex = null
  ) {
    while ( true ) {
      WritePrompt();

      var value = Console.ReadLine()?.Trim();

      if ( string.IsNullOrWhiteSpace( value ) ) {
        DeletePreviousLine();
        WritePrompt();
        console.WritePromptValue( defaultValue );

        return defaultValue;
      }

      DeletePreviousLine();
      WritePrompt();

      if ( regex == null || regex.IsMatch( value ) ) {
        console.WritePromptValue( $"{Chars.Checkmark} " + value );
        return value;
      }

      console.Write( TextHelper.Bold( value ), ConsoleColor.Cyan );
      console.WriteLine( $"{value} is not a valid value" );
    }

    void WritePrompt() {
      console.Write( $"{question} " );
      console.Write( $"[{defaultValue}]: ", ConsoleColor.DarkBlue );
    }
  }


  internal static bool PromptBool( this INormalOutput console, string question, PromptOption defaultOption ) {
    var options = defaultOption == PromptOption.Yes ? "Y/n" : "y/N";
    while ( true ) {
      WritePrompt();

      var value = Console.ReadLine()?.Trim().ToLowerInvariant();

      if ( string.IsNullOrWhiteSpace( value ) ) {
        DeletePreviousLine();
        WritePrompt();
        console.WritePromptValue( TextHelper.Bold( defaultOption == PromptOption.Yes ? "yes" : "no" ) );

        return defaultOption == PromptOption.Yes;
      }

      DeletePreviousLine();
      WritePrompt();

      switch ( value ) {
        case "y" or "yes":
          console.WritePromptValue( TextHelper.Bold( "yes" ) );
          return true;
        case "n" or "no":
          console.WritePromptValue( TextHelper.Bold( "no" ) );
          return false;
        default:
          console.Write( TextHelper.Bold( value ), ConsoleColor.Cyan );
          console.WriteLine( " Try again!", ConsoleColor.Red );
          continue;
      }
    }

    void WritePrompt() {
      console.Write( $"{question} " );
      console.Write( $"[{options}]: ", ConsoleColor.DarkBlue );
    }
  }

  private static void WritePromptValue( this INormalOutput console, string value ) {
    console.WriteLine( TextHelper.Bold( value ), ConsoleColor.Cyan );
  }

  private static void DeletePreviousLine() {
    Console.SetCursorPosition( 0, Console.CursorTop - 1 );
  }
}