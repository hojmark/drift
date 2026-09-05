using Drift.Domain;
using Drift.Domain.Device.Addresses;
using DomainNetwork = Drift.Domain.Network;

namespace Drift.Cli.Tests.Utils.Agent;

/// <summary>
/// Configuration for the CLI in a test scenario.
/// Defines what the CLI can see locally (interfaces and devices).
/// </summary>
public sealed class CliConfiguration {
  /// <summary>
  /// Gets subnets visible to the CLI locally (what interfaces the CLI has).
  /// </summary>
  public List<CidrBlock> VisibleSubnets {
    get;
    init;
  } = [];

  /// <summary>
  /// Gets devices that the CLI will discover when scanning its local subnets.
  /// </summary>
  public Dictionary<CidrBlock, List<DeviceAddressSet>> DiscoveredDevices {
    get;
    init;
  } = new();

  /// <summary>
  /// Gets network spec (optional) - devices declared in the spec file.
  /// </summary>
  public DomainNetwork? Network {
    get;
    init;
  }

  /// <summary>
  /// Gets the IDs of agents that the CLI can reach based on network topology and firewall rules.
  /// <c>null</c> means all agents are reachable (no CLI subnet defined, or no firewall configured).
  /// </summary>
  public IReadOnlyList<AgentId>? ReachableAgents {
    get;
    init;
  }
}