namespace Drift.Cli.Presentation.Console.Managers.Outputs;

internal partial class NormalOutput {
  public void WriteVeryVerbose(
    string text,
    ConsoleColor? foreground = ConsoleColor.DarkGray,
    ConsoleColor? background = null
  ) {
    if ( veryVerbose ) {
      WriteInternal( stdOut, 0, text, foreground, background );
    }
  }

  public void WriteLineVeryVerbose() {
    if ( veryVerbose ) {
      stdOut.WriteLine();
    }
  }

  public void WriteLineVeryVerbose(
    string text,
    ConsoleColor? foreground = ConsoleColor.DarkGray,
    ConsoleColor? background = null
  ) {
    if ( veryVerbose ) {
      WriteLineInternal( stdOut, 0, text, foreground, background );
    }
  }
}