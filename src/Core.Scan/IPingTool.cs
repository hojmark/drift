using Microsoft.Extensions.Logging;

namespace Drift.Core.Scan;

public interface IPingTool {
  public Task<(string StdOut, string ErrOut, int ExitCode, bool Cancelled)> RunAsync(
    string arguments,
    bool? logCommand = false,
    ILogger? logger = null
  );
}