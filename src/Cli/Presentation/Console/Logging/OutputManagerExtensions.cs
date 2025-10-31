using Drift.Cli.Presentation.Console.Managers.Abstractions;
using Drift.Common.Logging;

namespace Drift.Cli.Presentation.Console.Logging;

internal static class OutputManagerExtensions {
  /// <summary>
  /// Gets a compound logger instance that writes to both the <see cref="IOutputManager.Normal"/> and
  /// <see cref="IOutputManager.Log"/> outputs.
  /// </summary>
  public static ILogger GetLogger( this IOutputManager outputManager ) {
    return new CompoundLogger( [new NormalOutputLoggerAdapter( outputManager.Normal ), outputManager.Log] );
  }
}