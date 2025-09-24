namespace Drift.Cli.Presentation.Console.Managers.Outputs;

// Note: warnings go to std out
internal partial class NormalOutput {
  public void WriteWarning(
    string text,
    ConsoleColor? foreground = ConsoleColor.Yellow,
    ConsoleColor? background = null
  ) {
    WriteInternal( stdOut, 0, text, foreground, background );
  }

  public void WriteLineWarning() {
    stdOut.WriteLine();
  }

  public void WriteLineWarning(
    string text,
    ConsoleColor? foreground = ConsoleColor.Yellow,
    ConsoleColor? background = null
  ) {
    WriteLineInternal( stdOut, 0, text, foreground, background );
  }
}