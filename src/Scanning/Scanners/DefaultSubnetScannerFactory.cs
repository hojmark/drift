using System.Runtime.InteropServices;
using Drift.Domain;
using Drift.Domain.Scan;

namespace Drift.Scanning.Scanners;

public class DefaultSubnetScannerFactory(
  IPingTool pingTool
  /*IAgentClient agentClient,*/
  // IEnumerable<CidrBlock> localSubnets
) : ISubnetScannerFactory {
  private const bool UseFping = false;

  public ISubnetScanner Get( CidrBlock cidr ) {
    /*return localSubnets.Contains( cidr )
      ? new LocalSubnetScanner( _pingTool )
      : new RemoteSubnetScanner( _agentClient );*/

    if ( RuntimeInformation.IsOSPlatform( OSPlatform.Linux ) ) {
      return UseFping ? new LinuxFpingSubnetScanner() : new LinuxPingSubnetScanner( pingTool );
    }

    if ( RuntimeInformation.IsOSPlatform( OSPlatform.Windows ) ) {
      return new WindowsPingSubnetScanner();
    }

    throw new PlatformNotSupportedException();
  }
}