using System.Collections.Concurrent;

namespace Drift.Cli.Commands.Preview.Agent.Subcommands.Utils;

internal sealed class PredefinedLoggerProvider : ILoggerProvider {
  private readonly ILogger _logger;
  // private readonly ConcurrentDictionary<string, CustomConsoleLogger> _loggers = new();

  // TODO support category
  public PredefinedLoggerProvider( ILogger logger ) {
    _logger = logger;
  }

  public ILogger CreateLogger( string categoryName ) {
    return _logger;
  }

  public void Dispose() {
    // _loggers.Clear();
  }
}