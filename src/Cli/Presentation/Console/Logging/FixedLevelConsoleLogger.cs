using Microsoft.Extensions.Logging;

namespace Drift.Cli.Presentation.Console.Logging;

internal class FixedLevelConsoleLogger(
  LogLevel minLevel,
  bool includeStackTrace,
  TextWriter? stdOut = null,
  TextWriter? stdErr = null
) : ConsoleLogger( includeStackTrace, stdOut, stdErr ) {
  public override bool IsEnabled( LogLevel logLevel ) {
    return logLevel >= minLevel && base.IsEnabled( logLevel );
  }
}