using System.Net;
using System.Runtime.Versioning;
using Drift.Common;
using Microsoft.Extensions.Logging;

namespace Drift.Scanning;

//TODO should be possible to make internal by adding to dependency injection via this project
[SupportedOSPlatform( "linux" )]
public class LinuxPingTool : IPingTool {
  public async Task<PingResult> PingAsync(
    IPAddress ip,
    ILogger logger,
    CancellationToken cancellationToken = default
  ) {
    var tool = new ToolWrapper( "ping" );
    var result = await tool.ExecuteAsync( $"-c 1 -W 1 {ip}", logger, cancellationToken );
    return new PingResult( result.ExitCode == 0 );
  }
}