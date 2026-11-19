namespace Drift.Cli.Presentation.Console.Managers.Outputs;

internal partial class NormalOutput {
  public void WriteVeryVerbose(
    string text
  ) {
    if ( veryVerbose ) {
      WriteInternal( stdOut, 0, text, ConsoleColor.DarkGray );
    }
  }

  public void WriteLineVeryVerbose() {
    if ( veryVerbose ) {
      stdOut.WriteLine();
    }
  }

  public void WriteLineVeryVerbose(
    string text
  ) {
    if ( veryVerbose ) {
      WriteLineInternal( stdOut, 0, text, ConsoleColor.DarkGray );
    }
  }
}