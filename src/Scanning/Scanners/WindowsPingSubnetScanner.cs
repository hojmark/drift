using System.Net;
using System.Runtime.Versioning;
using Microsoft.Extensions.Logging;

namespace Drift.Scanning.Scanners;

[SupportedOSPlatform( "windows" )]
internal sealed class WindowsPingSubnetScanner : PingSubnetScannerBase {
  protected override Task<bool> PingAsync( IPAddress ip, ILogger logger, CancellationToken cancellationToken ) {
    throw new NotImplementedException();
  }
}