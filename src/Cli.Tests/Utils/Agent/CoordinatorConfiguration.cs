using Drift.Domain;
using Drift.Domain.Device.Addresses;

namespace Drift.Cli.Tests.Utils.Agent;

/// <summary>
/// Configuration for the CLI in a test scenario.
/// </summary>
internal sealed class CoordinatorConfiguration {
  /// <summary>
  /// Gets subnets visible to the coordinator locally (what interfaces the coordinator has).
  /// </summary>
  public List<CidrBlock> VisibleSubnets {
    get;
    init;
  } = [];

  /// <summary>
  /// Gets devices that the coordinator will discover when scanning its local subnets.
  /// </summary>
  public Dictionary<CidrBlock, List<DeviceAddressSet>> DiscoveredDevices {
    get;
    init;
  } = new();
}