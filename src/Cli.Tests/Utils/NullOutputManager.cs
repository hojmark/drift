using Drift.Cli.Output;
using Drift.Cli.Output.Abstractions;
using Microsoft.Extensions.Logging;

namespace Drift.Cli.Tests.Utils;

internal class NullOutputManager : IOutputManager {
  public ILogOutput Log {
    get;
  } = new NullOutput();

  public INormalOutput Normal {
    get;
  } = new NullOutput();

  public IJsonOutput Json {
    get;
  } = new NullOutput();

  public void WithNormalOutput( Action<INormalOutput> output ) {
  }

  public void WithLogOutput( Action<ILogOutput> output ) {
  }

  public void WithJsonOutput( Action<IJsonOutput> output ) {
  }

  public bool Is( OutputFormat outputFormat ) {
    return outputFormat == OutputFormat.Normal;
  }
}

internal class NullOutput : INormalOutput, ILogOutput, IJsonOutput {
  public void WriteVerbose( string text, ConsoleColor? foreground, ConsoleColor? background = null ) {
    // No-op
  }

  public void WriteLineVerbose() {
    // No-op
  }

  public void WriteLineVerbose( string text, ConsoleColor? foreground, ConsoleColor? background = null ) {
    // No-op
  }

  public void Write( string text, ConsoleColor? foreground = null, ConsoleColor? background = null ) {
    // No-op
  }

  public void Write( int level, string text, ConsoleColor? foreground = null, ConsoleColor? background = null ) {
    // No-op
  }

  public void WriteLine() {
    // No-op
  }

  public void WriteLine( string text, ConsoleColor? foreground = null, ConsoleColor? background = null ) {
    // No-op
  }

  public void WriteLine( int level, string text, ConsoleColor? foreground = null, ConsoleColor? background = null ) {
    // No-op
  }

  public void WriteWarning( string text, ConsoleColor? foreground, ConsoleColor? background = null ) {
    // No-op
  }

  public void WriteLineWarning() {
    // No-op
  }

  public void WriteLineWarning( string text, ConsoleColor? foreground, ConsoleColor? background = null ) {
    // No-op
  }

  public void WriteError( string text, ConsoleColor? foreground, ConsoleColor? background = null ) {
    // No-op
  }

  public void WriteLineError() {
    // No-op
  }

  public void WriteLineError( string text, ConsoleColor? foreground, ConsoleColor? background = null ) {
    // No-op
  }

  public void Log<TState>( LogLevel logLevel, EventId eventId, TState state, Exception? exception,
    Func<TState, Exception?, string> formatter ) {
    // No-op
  }

  public bool IsEnabled( LogLevel logLevel ) {
    return false;
  }

  public IDisposable? BeginScope<TState>( TState state ) where TState : notnull {
    return null;
  }
}