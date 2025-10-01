using System.Collections.Immutable;
using Drift.Domain.Device.Addresses;
using Drift.Domain.Device.Discovered;

namespace Drift.Domain.Scan;

public class SubnetScanResult : ScanResultBase {
  public required CidrBlock CidrBlock {
    get;
    init;
  }

  public IReadOnlyCollection<DiscoveredDevice> DiscoveredDevices {
    get;
    init;
  } = [];

  public IReadOnlySet<IpV4Address> DiscoveryAttempts {
    get;
    init;
  } = ImmutableHashSet<IpV4Address>.Empty;
}