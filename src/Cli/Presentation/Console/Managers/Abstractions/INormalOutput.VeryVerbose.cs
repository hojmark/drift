namespace Drift.Cli.Presentation.Console.Managers.Abstractions;

internal partial interface INormalOutput {
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
}