using System.Runtime.InteropServices;
using Drift.Domain;
using Drift.Domain.Scan;

namespace Drift.Scanning.Scanners;

public class SubnetScannerFactory(
  IPingTool pingTool
  /*IAgentClient agentClient,*/
  //IEnumerable<CidrBlock> localSubnets
) {
  private const bool UseFping = false;

  public ISubnetScanner GetScanner( CidrBlock cidr ) {
    /*return localSubnets.Contains( cidr )
      ? new LocalSubnetScanner( _pingTool )
      : new RemoteSubnetScanner( _agentClient );*/

    return
      RuntimeInformation.IsOSPlatform( OSPlatform.Linux )
        ? UseFping ? new LinuxFpingSubnetScanner() : new LinuxPingSubnetScanner( pingTool )
        : RuntimeInformation.IsOSPlatform( OSPlatform.Windows )
          ? new WindowsPingSubnetScanner()
          : throw new PlatformNotSupportedException();
  }
}