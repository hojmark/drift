using Drift.Utils;
using Microsoft.Extensions.Logging;

namespace Drift.Cli.Tools;

internal static class Nmap {
  internal static string ToolPath => "nmap";

  internal static Task<(string StdOut, string ErrOut, int ExitCode, bool Cancelled)> RunAsync(
    string arguments,
    bool? logCommand = false,
    ILogger? logger = null
  ) {
    if ( logCommand.HasValue && logCommand.Value ) {
      logger?.LogDebug( "Executing: {Tool} {Arguments}", ToolPath, arguments );
    }

    var tool = new ToolWrapper( ToolPath );
    return tool.ExecuteAsync( arguments );
  }
}