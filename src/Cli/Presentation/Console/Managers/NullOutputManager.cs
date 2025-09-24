using Drift.Cli.Presentation.Console.Managers.Abstractions;
using Microsoft.Extensions.Logging;
using Spectre.Console;

namespace Drift.Cli.Presentation.Console.Managers;

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

  public TextReader GetReader() {
    return new StringReader( string.Empty );
  }

  public void WithNormalOutput( Action<INormalOutput> action ) {
  }

  public void WithLogOutput( Action<ILogOutput> action ) {
  }

  public void WithJsonOutput( Action<IJsonOutput> action ) {
  }

  public bool Is( OutputFormat format ) {
    return format == OutputFormat.Normal;
  }
}

internal class NullOutput : INormalOutput, ILogOutput, IJsonOutput {
  public void WriteVeryVerbose(
    string text,
    ConsoleColor? foreground = ConsoleColor.DarkGray,
    ConsoleColor? background = null
  ) {
    // No-op
  }

  public void WriteLineVeryVerbose() {
    // No-op
  }

  public void WriteLineVeryVerbose(
    string text,
    ConsoleColor? foreground = ConsoleColor.DarkGray,
    ConsoleColor? background = null
  ) {
    // No-op
  }

  public void WriteVerbose(
    string text,
    ConsoleColor? foreground = ConsoleColor.DarkGray,
    ConsoleColor? background = null
  ) {
    // No-op
  }

  public void WriteLineVerbose() {
    // No-op
  }

  public void WriteLineVerbose(
    string text,
    ConsoleColor? foreground = ConsoleColor.DarkGray,
    ConsoleColor? background = null
  ) {
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

  public void WriteWarning(
    string text,
    ConsoleColor? foreground = ConsoleColor.Yellow,
    ConsoleColor? background = null
  ) {
    // No-op
  }

  public void WriteLineWarning() {
    // No-op
  }

  public void WriteLineWarning(
    string text,
    ConsoleColor? foreground = ConsoleColor.Yellow,
    ConsoleColor? background = null
  ) {
    // No-op
  }

  public void WriteError(
    string text,
    ConsoleColor? foreground = ConsoleColor.Red,
    ConsoleColor? background = null
  ) {
    // No-op
  }

  public void WriteLineError() {
    // No-op
  }

  public void WriteLineError(
    string text,
    ConsoleColor? foreground = ConsoleColor.Red,
    ConsoleColor? background = null
  ) {
    // No-op
  }

  public IAnsiConsole GetAnsiConsole() {
    var settings = new AnsiConsoleSettings { Out = new AnsiConsoleOutput( TextWriter.Null ) };
    return AnsiConsole.Create( settings );
  }

  public void Log<TState>(
    LogLevel logLevel,
    EventId eventId,
    TState state,
    Exception? exception,
    Func<TState, Exception?, string> formatter
  ) {
    // No-op
  }

  public bool IsEnabled( LogLevel logLevel ) {
    return false;
  }

  public IDisposable? BeginScope<TState>( TState state ) where TState : notnull {
    return null;
  }
}