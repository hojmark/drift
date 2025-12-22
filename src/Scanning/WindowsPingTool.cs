using System.Net;
using System.Runtime.Versioning;
using Drift.Common;
using Microsoft.Extensions.Logging;

namespace Drift.Scanning;

// TODO should be possible to make internal by adding to dependency injection via this project
[SupportedOSPlatform( "windows" )]
public class WindowsPingTool : IPingTool {
  public async Task<PingResult> PingAsync(
    IPAddress ip,
    ILogger logger,
    CancellationToken cancellationToken = default
  ) {
    var tool = new ToolWrapper( "ping" );
    // n = Number of echo requests to send, w = Timeout in milliseconds to wait for each reply
    var result = await tool.ExecuteAsync( $"-n 1 -w 1 {ip}", logger, cancellationToken );
    return new PingResult( result.ExitCode == 0 );
  }
}