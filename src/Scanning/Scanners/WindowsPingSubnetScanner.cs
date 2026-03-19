using System.Net;
using System.Runtime.Versioning;
using Drift.Scanning.Arp;
using Microsoft.Extensions.Logging;

namespace Drift.Scanning.Scanners;

[SupportedOSPlatform( "windows" )]
internal sealed class WindowsPingSubnetScanner( IPingTool pingTool ) : PingSubnetScannerBase {
  private static readonly IArpTableProvider ArpTableProvider = new WindowsArpTableProvider();

  protected override IArpTableProvider ArpTables() {
    return ArpTableProvider;
  }

  protected override async Task<bool> PingAsync( IPAddress ip, ILogger logger, CancellationToken cancellationToken ) {
    return ( await pingTool.PingAsync( ip, logger, cancellationToken ) ).Success;
  }
}