using Microsoft.Extensions.Logging;

namespace Drift.Cli.Presentation.Console.Logging;

internal class FixedLevelConsoleLogger( LogLevel minLevel, bool includeStackTrace )
  : ConsoleLogger( includeStackTrace ) {
  public override bool IsEnabled( LogLevel logLevel ) {
    return logLevel >= minLevel && base.IsEnabled( logLevel );
  }
}