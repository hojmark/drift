using Spectre.Console;

namespace Drift.Cli.Presentation.Console.Managers.Abstractions;

internal interface INormalOutput {
  public void WriteVeryVerbose(
    string text,
    ConsoleColor? foreground = ConsoleColor.DarkGray,
    ConsoleColor? background = null
  );

  public void WriteLineVeryVerbose();

  public void WriteLineVeryVerbose(
    string text,
    ConsoleColor? foreground = ConsoleColor.DarkGray,
    ConsoleColor? background = null
  );

  public void WriteVerbose(
    string text,
    ConsoleColor? foreground = ConsoleColor.DarkGray,
    ConsoleColor? background = null
  );

  public void WriteLineVerbose();

  public void WriteLineVerbose(
    string text,
    ConsoleColor? foreground = ConsoleColor.DarkGray,
    ConsoleColor? background = null
  );

  public void Write(
    string text,
    ConsoleColor? foreground = null,
    ConsoleColor? background = null
  );

  public void Write(
    int level,
    string text,
    ConsoleColor? foreground = null,
    ConsoleColor? background = null
  );

  public void WriteLine();

  public void WriteLine(
    string text,
    ConsoleColor? foreground = null,
    ConsoleColor? background = null
  );

  internal void WriteLine(
    int level,
    string text,
    ConsoleColor? foreground = null,
    ConsoleColor? background = null
  );

  public void WriteWarning(
    string text,
    ConsoleColor? foreground = ConsoleColor.Yellow,
    ConsoleColor? background = null
  );

  public void WriteLineWarning();

  public void WriteLineWarning(
    string text,
    ConsoleColor? foreground = ConsoleColor.Yellow,
    ConsoleColor? background = null
  );

  public void WriteError(
    string text,
    ConsoleColor? foreground = ConsoleColor.Red,
    ConsoleColor? background = null
  );

  public void WriteLineError();

  public void WriteLineError(
    string text,
    ConsoleColor? foreground = ConsoleColor.Red,
    ConsoleColor? background = null
  );

  IAnsiConsole GetAnsiConsole();
}