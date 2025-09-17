using Drift.Cli.Output.Abstractions;
using Microsoft.Extensions.Logging;

namespace Drift.Cli.Output.Logging;

public static class OutputManagerExtensions {
  public static ILogger GetCompoundLogger( this IOutputManager outputManager ) {
    return new CompoundLogger( [new NormalOutputLoggerAdapter( outputManager.Normal ), outputManager.Log] );
  }
}