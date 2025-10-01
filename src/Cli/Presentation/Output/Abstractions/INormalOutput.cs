using Spectre.Console;

namespace Drift.Cli.Presentation.Output.Abstractions;

internal partial interface INormalOutput {
  #region Verbose

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

  #endregion

  #region Info

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

  #endregion

  //TODO move note
  // Note: warnings go to std out

  #region Warning

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

  #endregion

  //TODO move note
  // Note: errors go to err out

  #region Error

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

  #endregion

  #region AnsiConsole

  IAnsiConsole GetAnsiConsole();

  #endregion
}