using Microsoft.Extensions.Logging;

namespace Drift.Cli.Output.Logging;

internal class CompoundLogger( IEnumerable<ILogger> loggers ) : ILogger {
  private readonly List<ILogger> _loggers = loggers.ToList();

  public void Log<TState>(
    LogLevel logLevel,
    EventId eventId,
    TState state,
    Exception? exception,
    Func<TState, Exception?, string> formatter
  ) {
    foreach ( var logger in _loggers ) {
      logger.Log( logLevel, eventId, state, exception, formatter );
    }
  }

  public bool IsEnabled( LogLevel logLevel ) {
    return _loggers.Any( logger => logger.IsEnabled( logLevel ) );
  }

  public IDisposable? BeginScope<TState>( TState state ) where TState : notnull {
    return null;
  }
}