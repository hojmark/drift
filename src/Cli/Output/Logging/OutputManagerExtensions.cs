using Drift.Cli.Output.Abstractions;
using Microsoft.Extensions.Logging;

namespace Drift.Cli.Output.Logging;

internal static class OutputManagerExtensions {
  /// <summary>
  /// Gets a compound logger instance that writes to both the <see cref="IOutputManager.Normal"/> and
  /// <see cref="IOutputManager.Log"/> outputs.
  /// </summary>
  public static ILogger GetLogger( this IOutputManager outputManager ) {
    return new CompoundLogger( [new NormalOutputLoggerAdapter( outputManager.Normal ), outputManager.Log] );
  }
}