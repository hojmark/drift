namespace Drift.Cli.Output;

internal partial class NormalOutput {
  // Use of banned Console APIs is OK. Main point is to centralize usage to this class.
#pragma warning disable RS0030

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

#pragma warning restore RS0030
}