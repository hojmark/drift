using Drift.Cli.Tests.Utils.Network.Firewall;
using Drift.Domain;
using Drift.Domain.Device.Addresses;

namespace Drift.Cli.Tests.Utils.Network.Topology;

/// <summary>
/// Represents a network topology for testing.
/// Supports optional firewall rules to control network visibility and simulate realistic network segmentation.
/// </summary>
public sealed class NetworkTopology {
  public required List<SubnetDefinition> Subnets {
    get;
    init;
  }

  /// <summary>
  /// Gets the CLI's network attachment definition.
  /// </summary>
  public CliDefinition? Cli {
    get;
    init;
  }

  public List<AgentDefinition> Agents {
    get;
    init;
  } = [];

  /// <summary>
  /// Gets optional firewall rules controlling network visibility.
  /// If null, all subnets can route to each other (flat network).
  /// If present, rules are evaluated in order with a default ALLOW policy.
  /// </summary>
  public FirewallRules? FirewallRules {
    get;
    init;
  }

  /// <summary>
  /// Gets optional firewall evaluator for subnet-aware rule evaluation.
  /// Constructed from FirewallRules + Subnets.
  /// </summary>
  public FirewallEvaluator? FirewallEvaluator {
    get;
    init;
  }

  /// <summary>
  /// Gets a value indicating whether check if this topology includes agents (distributed scenario).
  /// </summary>
  public bool HasAgents => Agents.Count > 0;

  /// <summary>
  /// Get the subnet the CLI is attached to, or null if no local interface is configured.
  /// </summary>
  public SubnetDefinition? GetCliSubnet() => Cli?.AttachedSubnet;

  /// <summary>
  /// Represents a subnet in the topology.
  /// </summary>
  public sealed class SubnetDefinition {
    public required string Name {
      get;
      init;
    }

    public required CidrBlock Cidr {
      get;
      init;
    }

    public required List<DeviceDefinition> Devices {
      get;
      init;
    }

    /// <summary>
    /// When true, devices on this subnet cannot communicate with each other (peer isolation).
    /// Mirrors UniFi's "Client Device Isolation" / "Device Isolation (ACL)" feature.
    /// Defaults to false (devices on the same subnet can communicate freely).
    /// </summary>
    public bool ClientIsolation {
      get;
      init;
    } = false;
  }

  /// <summary>
  /// Represents a device in the topology.
  /// </summary>
  public sealed class DeviceDefinition {
    public required IpV4Address Ip {
      get;
      init;
    }

    public required MacAddress Mac {
      get;
      init;
    }

    public required string Description {
      get;
      init;
    }
  }

  /// <summary>
  /// Represents the CLI's network attachment.
  /// </summary>
  public sealed class CliDefinition {
    public SubnetDefinition? AttachedSubnet {
      get;
      init;
    }
  }

  /// <summary>
  /// Represents an agent in the topology.
  /// </summary>
  public sealed class AgentDefinition {
    public required AgentId Id {
      get;
      init;
    }

    public required SubnetDefinition AttachedSubnet {
      get;
      init;
    }
  }
}