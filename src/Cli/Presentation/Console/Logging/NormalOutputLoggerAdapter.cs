using Drift.Cli.Presentation.Console.Managers.Abstractions;

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
        if ( exception != null ) {
          normalOutput.WriteLineError( exception.ToString() );
        }

        break;
      case LogLevel.Warning:
        normalOutput.WriteLineWarning( message );
        if ( exception != null ) {
          normalOutput.WriteLineWarning( exception.ToString() );
        }

        break;
      case LogLevel.Information:
        normalOutput.WriteLine( message );
        if ( exception != null ) {
          normalOutput.WriteLine( exception.ToString() );
        }

        break;
      case LogLevel.Debug:
        normalOutput.WriteLineVerbose( message );
        if ( exception != null ) {
          normalOutput.WriteLineVerbose( exception.ToString() );
        }

        break;
      case LogLevel.Trace:
        normalOutput.WriteLineVeryVerbose( message );
        if ( exception != null ) {
          normalOutput.WriteLineVeryVerbose( exception.ToString() );
        }

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