using Microsoft.Extensions.Logging;

namespace Drift.Core.Scan.Subnets.Interface;

public class PredefinedInterfaceSubnetProvider( List<INetworkInterface> interfaces, ILogger? logger = null )
  : InterfaceSubnetProviderBase( logger ) {
  public override List<INetworkInterface> GetInterfaces() {
    return interfaces;
  }
}