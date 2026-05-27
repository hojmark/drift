using Drift.Domain;
using Drift.Domain.Device.Addresses;

namespace Drift.Cli.Tests.Utils.Agent;

/// <summary>
/// Configuration for an agent in a test scenario.
/// </summary>
public sealed record AgentConfiguration {
  public required AgentId Id {
    get;
    init;
  }

  /// <summary>
  /// Gets HTTP address where the agent listens (e.g., http://localhost:51515).
  /// Assigned by <see cref="AgentTestHarness"/> after port allocation.
  /// </summary>
  public Uri Address {
    get;
    init;
  } = null!;

  /// <summary>
  /// Gets subnets visible to this agent (what interfaces/networks the agent can see).
  /// </summary>
  public List<CidrBlock> VisibleSubnets {
    get;
    init;
  } = [];

  /// <summary>
  /// Gets devices that this agent will discover when scanning its visible subnets.
  /// </summary>
  public Dictionary<CidrBlock, List<DeviceAddressSet>> DiscoveredDevices {
    get;
    init;
  } = new();

  /// <summary>
  /// Gets additional command-line arguments to pass when starting the agent.
  /// </summary>
  public string AdditionalArgs {
    get;
    init;
  } = "--adoptable";
}