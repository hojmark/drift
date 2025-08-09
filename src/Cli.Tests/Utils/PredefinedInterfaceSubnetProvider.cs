using Drift.Cli.Commands.Scan.Subnet;
using Drift.Cli.Output.Abstractions;

namespace Drift.Cli.Tests.Utils;

public class PredefinedInterfaceSubnetProvider( IOutputManager output, List<INetworkInterface> interfaces )
  : PhysicalInterfaceSubnetProvider( output ) {
  public override List<INetworkInterface> GetInterfaces() {
    return interfaces;
  }
}