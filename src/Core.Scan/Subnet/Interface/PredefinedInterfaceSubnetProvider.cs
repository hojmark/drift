using Microsoft.Extensions.Logging;

namespace Drift.Core.Scan.Subnet.Interface;

public class PredefinedInterfaceSubnetProvider( List<INetworkInterface> interfaces, ILogger? logger = null )
  : InterfaceSubnetProviderBase( logger ) {
  public override List<INetworkInterface> GetInterfaces() {
    return interfaces;
  }
}