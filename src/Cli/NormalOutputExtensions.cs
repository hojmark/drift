using System.Text.RegularExpressions;
using Drift.Cli.Commands.Init;
using Drift.Cli.Output.Abstractions;

namespace Drift.Cli;

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
        console.WriteValue( defaultValue );

        return defaultValue;
      }

      DeletePreviousLine();
      WritePrompt();

      if ( regex == null || regex.IsMatch( value ) ) {
        console.WriteValue( "✔ " + value );
        return value;
      }

      console.Write( InitCommand.ConsoleExtensions.Text.Bold( value ), ConsoleColor.Cyan );
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
        console.WriteValue(
          InitCommand.ConsoleExtensions.Text.Bold( defaultOption == PromptOption.Yes ? "yes" : "no" ) );

        return defaultOption == PromptOption.Yes;
      }

      DeletePreviousLine();
      WritePrompt();

      switch ( value ) {
        case "y" or "yes":
          console.WriteValue( InitCommand.ConsoleExtensions.Text.Bold( "yes" ) );
          return true;
        case "n" or "no":
          console.WriteValue( InitCommand.ConsoleExtensions.Text.Bold( "no" ) );
          return false;
        default:
          console.Write( InitCommand.ConsoleExtensions.Text.Bold( value ), ConsoleColor.Cyan );
          console.WriteLine( " Try again!", ConsoleColor.Red );
          continue;
      }
    }

    void WritePrompt() {
      console.Write( $"{question} " );
      console.Write( $"[{options}]: ", ConsoleColor.DarkBlue );
    }
  }

  private static void DeletePreviousLine() {
    Console.SetCursorPosition( 0, Console.CursorTop - 1 );
  }

  private static void WriteValue( this INormalOutput console, string value ) {
    console.WriteLine( InitCommand.ConsoleExtensions.Text.Bold( value ), ConsoleColor.Cyan );
  }

  internal enum PromptOption {
    Yes = 1,
    No = 2
  }
}