using System.Net;
using System.Runtime.Versioning;
using Drift.Utils;
using Microsoft.Extensions.Logging;

namespace Drift.Core.Scan;

//TODO should be possible to make internal by adding to dependency injection via this project
public class LinuxPingTool : IPingTool {
  private static string ToolPath => "ping";

  [SupportedOSPlatform( "linux" )]
  public Task<(string StdOut, string ErrOut, int ExitCode, bool Cancelled)> RunAsync(
    string arguments,
    ILogger logger,
    CancellationToken cancellationToken = default
  ) {
    var tool = new ToolWrapper( ToolPath );
    return tool.ExecuteAsync( arguments, logger, cancellationToken );
  }

  public async Task<PingResult> PingAsync(
    IPAddress ip,
    ILogger logger,
    CancellationToken cancellationToken = default
  ) {
    var result = await RunAsync( $"-c 1 -W 1 {ip}", logger, cancellationToken );
    return new PingResult( result.ExitCode == 0 );
  }
}