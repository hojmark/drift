using Microsoft.Extensions.Logging;

namespace Drift.TestUtilities;

internal sealed class TestLogger : ILogger {
  private readonly string _categoryName;

  public TestLogger( string categoryName = "" ) {
    _categoryName = categoryName;
  }

  public IDisposable? BeginScope<TState>( TState state ) where TState : notnull {
    return null;
  }

  public bool IsEnabled( LogLevel logLevel ) {
    return true;
  }

  public void Log<TState>(
    LogLevel logLevel,
    EventId eventId,
    TState state,
    Exception? exception,
    Func<TState, Exception?, string> formatter ) {
    TestContext.Out.WriteLine(
      $"[{ToSerilogStyleLevel( logLevel )}] {_categoryName}: {formatter( state, exception )}{( exception is not null ? " " + exception : string.Empty )}"
    );
  }

  private static string ToSerilogStyleLevel( LogLevel level ) => level switch {
    LogLevel.Trace => "TRC",
    LogLevel.Debug => "DBG",
    LogLevel.Information => "INF",
    LogLevel.Warning => "WRN",
    LogLevel.Error => "ERR",
    LogLevel.Critical => "FTL",
    LogLevel.None or _ => throw new Exception( "No mapping for log level " + level )
  };
}