using Drift.Cli.Tools;
using Microsoft.Extensions.Logging;

namespace Drift.Cli.Tests.Utils;

public class TestPingTool : IPingTool {
  public Task<(string StdOut, string ErrOut, int ExitCode, bool Cancelled)> RunAsync( string arguments,
    bool? logCommand, ILogger? logger = null ) {
    throw new NotImplementedException();
  }
}