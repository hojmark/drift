using System.Net;
using System.Runtime.Versioning;
using Microsoft.Extensions.Logging;

namespace Drift.Scanning.Scanners;

[SupportedOSPlatform( "linux" )]
internal sealed class LinuxPingSubnetScanner( IPingTool pingTool ) : PingSubnetScannerBase {
  protected override async Task<bool> PingAsync( IPAddress ip, ILogger logger, CancellationToken cancellationToken ) {
    return ( await pingTool.PingAsync( ip, logger, cancellationToken ) ).Success;
  }
}