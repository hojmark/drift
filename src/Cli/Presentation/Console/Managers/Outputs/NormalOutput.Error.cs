namespace Drift.Cli.Presentation.Console.Managers.Outputs;

// Note: errors go to err out
internal partial class NormalOutput {
  public void WriteError(
    string text,
    ConsoleColor? foreground = ConsoleColor.Red,
    ConsoleColor? background = null
  ) {
    WriteInternal( errOut, 0, text, foreground, background );
  }

  public void WriteLineError() {
    errOut.WriteLine();
  }

  public void WriteLineError(
    string text,
    ConsoleColor? foreground = ConsoleColor.Red,
    ConsoleColor? background = null
  ) {
    WriteLineInternal( errOut, 0, text, foreground, background );
  }
}