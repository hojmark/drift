namespace Drift.Cli.Presentation.Console.Managers.Outputs;

internal partial class NormalOutput {
  public void Write(
    string text,
    ConsoleColor? foreground = null,
    ConsoleColor? background = null
  ) {
    WriteInternal( stdOut, 0, text, foreground, background );
  }

  public void Write(
    int level,
    string text,
    ConsoleColor? foreground = null,
    ConsoleColor? background = null
  ) {
    WriteInternal( stdOut, level, text, foreground, background );
  }

  public void WriteLine() {
    stdOut.WriteLine();
  }

  public void WriteLine(
    string text,
    ConsoleColor? foreground = null,
    ConsoleColor? background = null
  ) {
    WriteLineInternal( stdOut, 0, text, foreground, background );
  }

  public void WriteLine(
    int level,
    string text,
    ConsoleColor? foreground = null,
    ConsoleColor? background = null
  ) {
    WriteLineInternal( stdOut, level, text, foreground, background );
  }
}