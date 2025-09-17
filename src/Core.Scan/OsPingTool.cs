using Drift.Utils;
using Microsoft.Extensions.Logging;

namespace Drift.Core.Scan;

//TODO should be possible to make internal by adding to dependency injection via this project
public class OsPingTool : IPingTool {
  private static string ToolPath => "ping";

  public Task<(string StdOut, string ErrOut, int ExitCode, bool Cancelled)> RunAsync(
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