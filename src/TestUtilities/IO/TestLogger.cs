using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Drift.TestUtilities.IO;

public sealed class TestLogger : ILogger {
  private readonly string _categoryName;
  private readonly bool _captureEntries;
  private readonly ConcurrentQueue<TestLogEntry> _entries = new();

  public IReadOnlyCollection<TestLogEntry> Entries => _captureEntries
    ? _entries.ToArray()
    : throw new InvalidOperationException( "Log entry capture is not enabled for this logger." );

  public TestLogger( string categoryName = "", bool captureEntries = false ) {
    _categoryName = categoryName;
    _captureEntries = captureEntries;
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
    Func<TState, Exception?, string> formatter
  ) {
    var message = formatter( state, exception );
    if ( _captureEntries ) {
      _entries.Enqueue( new TestLogEntry( logLevel, eventId, message, exception ) );
    }

    TestContext.Out.WriteLine(
      $"[{ToSerilogStyleLevel( logLevel )}] {_categoryName}: {message}{( exception is not null ? " " + exception : string.Empty )}"
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

public sealed record TestLogEntry( LogLevel Level, EventId EventId, string Message, Exception? Exception );