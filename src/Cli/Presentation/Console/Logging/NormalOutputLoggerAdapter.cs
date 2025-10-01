using Drift.Cli.Presentation.Console.Managers.Abstractions;
using Microsoft.Extensions.Logging;

namespace Drift.Cli.Presentation.Console.Logging;

internal class NormalOutputLoggerAdapter( INormalOutput normalOutput ) : ILogger {
  public void Log<TState>(
    LogLevel logLevel,
    EventId eventId,
    TState state,
    Exception? exception,
    Func<TState, Exception?, string> formatter
  ) {
    var message = formatter( state, exception );

    switch ( logLevel ) {
      case LogLevel.Critical:
      case LogLevel.Error:
        normalOutput.WriteLineError( message );
        break;
      case LogLevel.Warning:
        normalOutput.WriteLineWarning( message );
        break;
      case LogLevel.Information:
        normalOutput.WriteLine( message );
        break;
      case LogLevel.Debug:
        normalOutput.WriteLineVerbose( message );
        break;
      case LogLevel.Trace:
        normalOutput.WriteLineVeryVerbose( message );
        break;
      case LogLevel.None:
        break;
    }
  }

  public bool IsEnabled( LogLevel logLevel ) {
    return logLevel != LogLevel.None;
  }

  public IDisposable? BeginScope<TState>( TState state ) where TState : notnull {
    return null;
  }
}