using Drift.Domain.Device.Discovered;

namespace Drift.Core.Scan;

public class ScanResult {
  public required Metadata Metadata {
    get;
    init;
  }

  public required ScanResultStatus Status {
    get;
    init;
  }


  public IEnumerable<DiscoveredDevice> DiscoveredDevices {
    get;
    init;
  } = [];
}