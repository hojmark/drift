namespace Drift.Cli.Presentation.Console.Managers.Outputs;

internal partial class NormalOutput {
  public void WriteVerbose(
    string text,
    ConsoleColor? foreground = ConsoleColor.DarkGray,
    ConsoleColor? background = null
  ) {
    if ( verbose ) {
      WriteInternal( stdOut, 0, text, foreground, background );
    }
  }

  public void WriteLineVerbose() {
    if ( verbose ) {
      stdOut.WriteLine();
    }
  }

  public void WriteLineVerbose(
    string text,
    ConsoleColor? foreground = ConsoleColor.DarkGray,
    ConsoleColor? background = null
  ) {
    if ( verbose ) {
      WriteLineInternal( stdOut, 0, text, foreground, background );
    }
  }
}