using Drift.Core.Scan.Subnet;
using Microsoft.Extensions.Logging;

namespace Drift.Cli.Tests.Utils;

public class PredefinedInterfaceSubnetProvider( ILogger logger, List<INetworkInterface> interfaces )
  : InterfaceSubnetProviderBase( logger ) {
  public override List<INetworkInterface> GetInterfaces() {
    return interfaces;
  }
}