using Drift.Cli.Output.Abstractions;

namespace Drift.Cli.Output;

internal class NormalOutput( TextWriter stdOut, TextWriter errOut, bool verbose = false ) : INormalOutput {
  // Use of banned Console APIs is OK. Main point is to centralize usage to this class.
#pragma warning disable RS0030

  #region Verbose

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

  #endregion

  #region Info

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

  #endregion

  // Note: warnings go to std out

  #region Warning

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

  #endregion

  // Note: errors go to err out

  #region Error

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

  #endregion


  # region COMMON

  private static void WriteInternal(
    TextWriter textWriter,
    int level,
    string? text = null,
    ConsoleColor? foreground = null,
    ConsoleColor? background = null
  ) {
    textWriter.Write( new string( ' ', level * 2 ) );

    if ( foreground.HasValue ) Console.ForegroundColor = foreground.Value;
    if ( background.HasValue ) Console.BackgroundColor = background.Value;

    textWriter.Write( text );

    Console.ResetColor();
  }

  private static void WriteLineInternal(
    TextWriter textWriter,
    int level,
    string text,
    ConsoleColor? foreground = null,
    ConsoleColor? background = null
  ) {
    var lines = text.Split( '\n' );
    if ( lines[^1] == "" ) lines = lines.Take( lines.Length - 1 ).ToArray();
    foreach ( var line in lines ) {
      textWriter.Write( new string( ' ', level * 2 ) );

      if ( foreground.HasValue ) Console.ForegroundColor = foreground.Value;
      if ( background.HasValue ) Console.BackgroundColor = background.Value;

      textWriter.WriteLine( line );

      Console.ResetColor();
    }
  }

  #endregion

#pragma warning restore RS0030
}