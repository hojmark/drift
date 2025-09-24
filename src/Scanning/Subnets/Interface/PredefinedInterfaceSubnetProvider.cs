using Microsoft.Extensions.Logging;

namespace Drift.Scanning.Subnets.Interface;

public sealed class PredefinedInterfaceSubnetProvider( List<INetworkInterface> interfaces, ILogger? logger = null )
  : InterfaceSubnetProviderBase( logger ) {
  public override List<INetworkInterface> GetInterfaces() {
    return interfaces;
  }
}