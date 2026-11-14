using System.Collections.Immutable;
using System.Text.Json.Serialization;
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

  [JsonIgnore( Condition = JsonIgnoreCondition.WhenWritingNull )]
  public string? ErrorMessage {
    get;
    init;
  }

  public IReadOnlySet<IpV4Address> DiscoveryAttempts {
    get;
    init;
  } = ImmutableHashSet<IpV4Address>.Empty;
}