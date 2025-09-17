using Drift.Core.Scan.Subnet;
using Microsoft.Extensions.Logging;

namespace Drift.Cli.Tests.Utils;

public class PredefinedInterfaceSubnetProvider( List<INetworkInterface> interfaces, ILogger? logger = null )
  : InterfaceSubnetProviderBase( logger ) {
  public override List<INetworkInterface> GetInterfaces() {
    return interfaces;
  }
}