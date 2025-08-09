using Microsoft.Extensions.Logging;

namespace Drift.Cli.Tools;

internal interface IPingTool {
  internal Task<(string StdOut, string ErrOut, int ExitCode, bool Cancelled)> RunAsync(
    string arguments,
    bool? logCommand = false,
    ILogger? logger = null
  );
}