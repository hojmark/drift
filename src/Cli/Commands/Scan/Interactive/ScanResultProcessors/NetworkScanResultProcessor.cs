using Drift.Cli.Commands.Scan.Interactive.Models;
using Drift.Domain;
using Drift.Domain.Scan;

namespace Drift.Cli.Commands.Scan.Interactive.ScanResultProcessors;

internal static class NetworkScanResultProcessor {
  internal static List<Subnet> Process( NetworkScanResult scanResult, Network? network ) {
    return scanResult.Subnets
      .Select( subnet =>
        new Subnet { Cidr = subnet.CidrBlock, Devices = SubnetScanResultProcessor.Process( subnet, network ), }
      ).ToList();
  }
}