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

  /// <summary>
  /// Writes preview-mode warnings for agent support to all outputs.
  /// Applicable to both agent hosting and distributed scanning via agents.
  /// </summary>
  public static void WarnAgentPreview( this IOutputManager outputManager ) {
    var logger = outputManager.GetLogger();
    logger.LogWarning( "Distributed scanning via agents is a preview feature and should be used with caution." );
    logger.LogWarning( "Agent communication is unencrypted. Do not use on untrusted networks." );
    logger.LogWarning( "Agents run without authentication. Any client that can reach the agent port can connect." );
    logger.LogWarning( "Agent adoption (--adoptable / --join) is not yet implemented and has no effect." );
  }
}