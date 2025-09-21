using Drift.Domain;
using Drift.Domain.Scan;

namespace Drift.Core.Scan.Scanners;

public class DefaultSubnetScannerProvider(
  IPingTool pingTool
  /*IAgentClient agentClient,*/
  //IEnumerable<CidrBlock> localSubnets
) : ISubnetScannerProvider {
  public ISubnetScanner GetScanner( CidrBlock cidr ) {
    /*return localSubnets.Contains( cidr )
      ? new LocalSubnetScanner( _pingTool )
      : new RemoteSubnetScanner( _agentClient );*/
    return new LocalSubnetScanner( pingTool );
  }
}