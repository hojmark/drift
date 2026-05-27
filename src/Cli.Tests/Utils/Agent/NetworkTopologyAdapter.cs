using Drift.Cli.Tests.Utils.Network.Firewall;
using Drift.Cli.Tests.Utils.Network.Topology;
using Drift.Domain;
using Drift.Domain.Device.Addresses;

namespace Drift.Cli.Tests.Utils.Agent;

/// <summary>
/// Adapter for converting <see cref="NetworkTopology"/> to individual <see cref="AgentConfiguration"/>s
/// and <see cref="CliConfiguration"/>s.
/// Computes visibility based on firewall rules and generates configurations for test execution.
/// </summary>
internal static class NetworkTopologyAdapter {
  /// <summary>
  /// <see cref="NetworkTopology"/> to individual <see cref="AgentConfiguration"/>s.
  /// </summary>
  internal static List<AgentConfiguration> ToAgentConfigurations( NetworkTopology topology ) {
    var configs = new List<AgentConfiguration>();

    foreach ( var agent in topology.Agents ) {
      var visibleSubnets = GetAccessibleSubnets( topology, agent );
      var visibleDevices = GetAccessibleDevices( topology, agent );

      var devicesBySubnet = new Dictionary<CidrBlock, List<DeviceAddressSet>>();

      foreach ( var subnet in visibleSubnets ) {
        // Get devices that are in this subnet AND visible to the agent
        var devicesInSubnet = subnet.Devices
          .Where( visibleDevices.Contains )
          .Select( d => new DeviceAddressSet( ip: d.Ip, mac: d.Mac ) )
          .ToList();

        devicesBySubnet[subnet.Cidr] = devicesInSubnet;
      }

      configs.Add( new AgentConfiguration {
        Id = agent.Id,
        VisibleSubnets = visibleSubnets.Select( s => s.Cidr ).ToList(),
        DiscoveredDevices = devicesBySubnet
      } );
    }

    return configs;
  }

  /// <summary>
  /// <see cref="NetworkTopology"/> to individual <see cref="CliConfiguration"/>s.
  /// </summary>
  internal static CliConfiguration ToCliConfiguration( NetworkTopology topology ) {
    var cliSubnet = topology.GetCliSubnet();

    var devicesBySubnet = new Dictionary<CidrBlock, List<DeviceAddressSet>>();

    if ( cliSubnet != null ) {
      var devicesInSubnet = cliSubnet.Devices
        .Select( d => new DeviceAddressSet( ip: d.Ip, mac: d.Mac ) )
        .ToList();

      devicesBySubnet[cliSubnet.Cidr] = devicesInSubnet;
    }

    // Compute which agents CLI can actually reach based on firewall rules.
    // null means all agents are reachable (no CLI subnet or no firewall = flat/management network).
    IReadOnlyList<AgentId>? reachableAgentIds = null;

    if ( cliSubnet != null && topology.FirewallEvaluator != null ) {
      reachableAgentIds = GetAccessibleAgents( topology, cliSubnet )
        .Select( agent => agent.Id )
        .ToList();
    }

    return new CliConfiguration {
      VisibleSubnets = cliSubnet != null ? [cliSubnet.Cidr] : [],
      DiscoveredDevices = devicesBySubnet,
      Network = null, // Can be extended if needed
      ReachableAgents = reachableAgentIds
    };
  }

  /// <summary>
  /// Returns subnets accessible to an agent given its attachment point and firewall rules.
  /// Directly attached subnet is always accessible unless ClientIsolation is enabled.
  /// Firewall governs cross-subnet (routed) traffic only.
  /// If no firewall is configured, all subnets are accessible (flat routed network).
  /// </summary>
  private static List<NetworkTopology.SubnetDefinition> GetAccessibleSubnets(
    NetworkTopology topology,
    NetworkTopology.AgentDefinition agent
  ) {
    if ( topology.FirewallEvaluator == null ) {
      return topology.Subnets;
    }

    var accessible = new HashSet<NetworkTopology.SubnetDefinition>();

    if ( !agent.AttachedSubnet.ClientIsolation ) {
      accessible.Add( agent.AttachedSubnet );
    }

    foreach ( var targetSubnet in topology.Subnets ) {
      if ( targetSubnet == agent.AttachedSubnet ) {
        continue;
      }

      if ( topology.FirewallEvaluator.IsAllowed(
            FirewallTarget.Subnet( agent.AttachedSubnet.Name ),
            FirewallTarget.Subnet( targetSubnet.Name ) ) ) {
        accessible.Add( targetSubnet );
      }
    }

    return accessible.ToList();
  }

  /// <summary>
  /// Returns devices accessible to an agent given its attachment point and firewall rules.
  /// Devices on the directly attached subnet are accessible unless ClientIsolation is enabled.
  /// Firewall governs cross-subnet (routed) traffic only.
  /// </summary>
  private static List<NetworkTopology.DeviceDefinition> GetAccessibleDevices(
    NetworkTopology topology,
    NetworkTopology.AgentDefinition agent
  ) {
    var accessible = new List<NetworkTopology.DeviceDefinition>();

    foreach ( var subnet in topology.Subnets ) {
      bool isSameSubnet = subnet == agent.AttachedSubnet;

      foreach ( var device in subnet.Devices ) {
        bool canAccess = isSameSubnet
          ? !subnet.ClientIsolation
          : ( topology.FirewallEvaluator?.IsAllowed(
            FirewallTarget.Subnet( agent.AttachedSubnet.Name ),
            FirewallTarget.FromIp( device.Ip ) ) ?? true );

        if ( canAccess ) {
          accessible.Add( device );
        }
      }
    }

    return accessible;
  }

  /// <summary>
  /// Returns agents accessible from the CLI's subnet through the firewall.
  /// </summary>
  private static List<NetworkTopology.AgentDefinition> GetAccessibleAgents(
    NetworkTopology topology,
    NetworkTopology.SubnetDefinition cliSubnet
  ) {
    return topology.Agents
      .Where( agent => topology.FirewallEvaluator!.IsAllowed(
        FirewallTarget.Subnet( cliSubnet.Name ),
        FirewallTarget.Subnet( agent.AttachedSubnet.Name ) )
      )
      .ToList();
  }
}