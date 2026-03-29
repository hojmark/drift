using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace Drift.Cli.Presentation.Console.Logging;

[SuppressMessage(
  "ApiDesign",
  "RS0030:Do not use banned APIs",
  Justification = "Fallback logger for use before DI is available"
)]
internal class ConsoleLogger( bool includeStackTrace = true, TextWriter? stdOut = null, TextWriter? stdErr = null )
  : ILogger {
  private TextWriter Out => stdOut ?? System.Console.Out;
  private TextWriter Err => stdErr ?? System.Console.Error;

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

    var formattedMessage = formatter( state, exception );

    var color = GetColor( logLevel );
    if ( color != null ) {
      System.Console.ForegroundColor = color.Value;
    }

    if ( logLevel >= LogLevel.Error ) {
      Err.WriteLine( formattedMessage );
    }
    else {
      Out.WriteLine( formattedMessage );
    }

    System.Console.ResetColor();

    if ( exception != null ) {
      System.Console.ForegroundColor = ConsoleColor.Red;
      Err.WriteLine( exception.Message );

      if ( includeStackTrace ) {
        System.Console.ForegroundColor = ConsoleColor.Gray;
        Err.WriteLine( exception.StackTrace );
      }

      System.Console.ResetColor();
    }
  }

  public virtual bool IsEnabled( LogLevel logLevel ) {
    return logLevel != LogLevel.None;
  }

  public IDisposable? BeginScope<TState>( TState state ) where TState : notnull {
    return null;
  }

  private static ConsoleColor? GetColor( LogLevel logLevel ) {
    return logLevel switch {
      LogLevel.Trace => ConsoleColor.DarkGray,
      LogLevel.Debug => ConsoleColor.DarkGray,
      LogLevel.Information => null,
      LogLevel.Warning => ConsoleColor.Yellow,
      LogLevel.Error => ConsoleColor.Red,
      LogLevel.Critical => ConsoleColor.Red,
      _ => throw new Exception( "No mapping for log level " + logLevel )
    };
  }
}