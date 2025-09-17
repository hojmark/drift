using Drift.Domain.Device.Discovered;

namespace Drift.Domain.Scan;

public class SubnetScanResult {
  public required Metadata Metadata {
    get;
    init;
  }

  public required ScanResultStatus Status {
    get;
    init;
  }

  public required CidrBlock CidrBlock {
    get;
    init;
  }

  public IEnumerable<DiscoveredDevice> DiscoveredDevices {
    get;
    init;
  } = [];

  public Percentage Progress {
    get;
    init;
  } = new(0);
}