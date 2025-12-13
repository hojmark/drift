using System.Net;
using System.Runtime.Versioning;
using Drift.Scanning.Arp;
using Microsoft.Extensions.Logging;

namespace Drift.Scanning.Scanners;

[SupportedOSPlatform( "windows" )]
internal sealed class WindowsPingSubnetScanner( IPingTool pingTool ) : PingSubnetScannerBase {
  protected override IArpTableProvider ArpTables() {
    throw new NotImplementedException();
  }

  protected override Task<bool> PingAsync( IPAddress ip, ILogger logger, CancellationToken cancellationToken ) {
    throw new NotImplementedException();
  }
}