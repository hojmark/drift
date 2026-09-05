namespace Drift.Cli.Presentation.Console.Managers.Outputs;

internal partial class NormalOutput {
  public void WriteVerbose(
    string text
  ) {
    if ( verbose ) {
      WriteInternal( stdOut, 0, text, ConsoleColor.DarkGray );
    }
  }

  public void WriteLineVerbose() {
    if ( verbose ) {
      stdOut.WriteLine();
    }
  }

  public void WriteLineVerbose(
    string text
  ) {
    if ( verbose ) {
      WriteLineInternal( stdOut, 0, text, ConsoleColor.DarkGray );
    }
  }
}