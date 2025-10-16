using Microsoft.Extensions.Logging;

namespace Drift.TestUtilities;

public sealed class StringLogger( StringWriter? writer = null ) : ILogger {
  private readonly StringWriter _writer = writer ?? new StringWriter();

  public void Log<TState>(
    LogLevel logLevel,
    EventId eventId,
    TState state,
    Exception? exception,
    Func<TState, Exception?, string> formatter
  ) {
    if ( !IsEnabled( logLevel ) ) {
      return;
    }

    var message = formatter( state, exception );
    var logEntry = $"[{ToSerilogStyleLevel( logLevel )}] {message}";

    if ( exception != null ) {
      logEntry += System.Environment.NewLine + exception;
    }

    _writer.WriteLine( logEntry );
  }

  public bool IsEnabled( LogLevel logLevel ) {
    return logLevel != LogLevel.None;
  }

  public IDisposable? BeginScope<TState>( TState state ) where TState : notnull {
    return null;
  }

  public override string ToString() {
    return _writer.ToString();
  }

  private static string ToSerilogStyleLevel( LogLevel logLevel ) {
    return logLevel switch {
      LogLevel.Trace => "VRB",
      LogLevel.Debug => "DBG",
      LogLevel.Information => "INF",
      LogLevel.Warning => "WRN",
      LogLevel.Error => "ERR",
      LogLevel.Critical => "FTL",
      _ => throw new Exception( "No mapping for log level " + logLevel )
    };
  }
}