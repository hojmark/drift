using System.Net;
using System.Runtime.Versioning;
using Microsoft.Extensions.Logging;

namespace Drift.Core.Scan.Scanners;

[SupportedOSPlatform( "windows" )]
public class WindowsPingSubnetScanner : PingSubnetScannerBase {
  protected override Task<bool> PingAsync( IPAddress ip, ILogger logger, CancellationToken cancellationToken ) {
    throw new NotImplementedException();
  }
}