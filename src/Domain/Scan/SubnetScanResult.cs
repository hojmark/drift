using Drift.Domain.Device.Discovered;

namespace Drift.Domain.Scan;

public class SubnetScanResult : ScanResultBase {
  public required CidrBlock CidrBlock {
    get;
    init;
  }

  public IEnumerable<DiscoveredDevice> DiscoveredDevices {
    get;
    init;
  } = [];
}