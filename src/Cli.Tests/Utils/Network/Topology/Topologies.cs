using Drift.Cli.Tests.Utils.Network.Firewall;
using Drift.Domain;
using Drift.Domain.Device.Addresses;

namespace Drift.Cli.Tests.Utils.Network.Topology;

/// <summary>
/// Pre-configured topology scenarios for common test cases.
/// Returns NetworkTopology instances that can be used with AgentTestHarness.CreateFromTopologyAsync().
/// </summary>
public static class Topologies {
  /// <summary>
  /// Two agents, each on different subnets, CLI on its own subnet.
  /// No firewall = flat routed network (all can see all).
  /// Tests basic distributed scanning with CLI participation.
  /// </summary>
  public static NetworkTopologyBuilder TwoAgentsDisjointSubnetsWithCli() {
    return new NetworkTopologyBuilder()
      .AddSubnet( "cli-subnet", "192.168.0.0/24", [( "192.168.0.100", "CLI device" )], out var cliSubnet )
      .AddSubnet(
        "agent1-subnet",
        "192.168.10.0/24",
        [
          ( "192.168.10.100", "Agent1 device 1" ),
          ( "192.168.10.101", "Agent1 device 2" )
        ],
        out var agent1Subnet
      )
      .AddSubnet(
        "agent2-subnet",
        "192.168.20.0/24",
        [
          ( "192.168.20.100", "Agent2 device" )
        ],
        out var agent2Subnet
      )
      .AddAgent( new AgentId( "agentid_agent1" ), agent1Subnet )
      .AddAgent( new AgentId( "agentid_agent2" ), agent2Subnet )
      .WithCli( cliSubnet );
  }

  /// <summary>
  /// Two agents, each on different subnets, CLI has no local interfaces.
  /// No firewall = flat routed network.
  /// Tests agents-only scanning without local CLI scanner.
  /// </summary>
  public static NetworkTopology TwoAgentsDisjointSubnetsNoCli() {
    return new NetworkTopologyBuilder()
      .AddSubnet( "agent1-subnet", "192.168.10.0/24", [
        ( "192.168.10.100", "Agent1 device 1" ),
        ( "192.168.10.101", "Agent1 device 2" )
      ], out var agent1Subnet )
      .AddSubnet( "agent2-subnet", "192.168.20.0/24", [
        ( "192.168.20.100", "Agent2 device" )
      ], out var agent2Subnet )
      .AddAgent( new AgentId( "agentid_agent1" ), agent1Subnet )
      .AddAgent( new AgentId( "agentid_agent2" ), agent2Subnet )
      .Build();
  }

  /// <summary>
  /// Two agents on the SAME subnet with CLI on different subnet.
  /// No firewall = flat routed network.
  /// Tests result merging when multiple agents scan overlapping networks.
  /// </summary>
  public static NetworkTopology TwoAgentsOverlappingSubnet() {
    return new NetworkTopologyBuilder()
      .AddSubnet( "cli-subnet", "192.168.0.0/24", [
        ( "192.168.0.100", "CLI device" )
      ], out var cliSubnet )
      .AddSubnet( "shared-subnet", "192.168.10.0/24", [
        ( "192.168.10.100", "Shared device 1" ),
        ( "192.168.10.101", "Shared device 2" ),
        ( "192.168.10.102", "Shared device 3" ),
        ( "192.168.10.103", "Shared device 4" )
      ], out var sharedSubnet )
      .AddAgent( new AgentId( "agentid_agent1" ), sharedSubnet )
      .AddAgent( new AgentId( "agentid_agent2" ), sharedSubnet )
      .WithCli( cliSubnet )
      .Build();
  }

  /// <summary>
  /// Single agent with empty subnet (no devices).
  /// Tests handling of empty scan results.
  /// </summary>
  public static NetworkTopology SingleAgentEmptyResults() {
    return new NetworkTopologyBuilder()
      .AddSubnet( "empty-subnet", "192.168.10.0/24", [], out var emptySubnet ) // No devices
      .AddAgent( new AgentId( "agentid_agent1" ), emptySubnet )
      .Build();
  }

  /// <summary>
  /// CLI + one agent on different subnets.
  /// No firewall = flat routed network.
  /// Tests mixed local and distributed scanning.
  /// </summary>
  public static NetworkTopology MixedCliAndAgents() {
    return new NetworkTopologyBuilder()
      .AddSubnet( "cli-subnet", "192.168.0.0/24", [
        ( "192.168.0.100", "CLI device" )
      ], out var cliSubnet )
      .AddSubnet( "agent-subnet", "192.168.10.0/24", [
        ( "192.168.10.100", "Agent device" )
      ], out var agentSubnet )
      .AddAgent( new AgentId( "agentid_agent1" ), agentSubnet )
      .WithCli( cliSubnet )
      .Build();
  }

  // ========== FIREWALL SCENARIOS ==========

  /// <summary>
  /// DMZ with firewall rules.
  /// DMZ subnet can reach internal subnet, but internal cannot initiate connections to DMZ.
  /// Tests unidirectional firewall rules.
  /// </summary>
  public static NetworkTopology DmzWithFirewall() {
    return new NetworkTopologyBuilder()
      .AddSubnet( "dmz", "192.168.1.0/24", [
        ( "192.168.1.100", "Web server" ),
        ( "192.168.1.101", "App server" )
      ], out var dmzSubnet )
      .AddSubnet( "internal", "10.0.0.0/24", [
        ( "10.0.0.100", "Database server" ),
        ( "10.0.0.101", "File server" )
      ], out var internalSubnet )
      .AddAgent( new AgentId( "agentid_dmz" ), dmzSubnet )
      .AddAgent( new AgentId( "agentid_internal" ), internalSubnet )
      // Firewall rules:
      .AllowConnection( "dmz", "dmz" ) // DMZ can see itself
      .AllowConnection( "dmz", "internal" ) // DMZ → internal allowed
      .AllowConnection( "internal", "internal" ) // Internal can see itself
      // internal → dmz NOT allowed (not specified, will fall through to default ALLOW)
      // So we need to explicitly deny:
      .DenyConnection( "internal", "dmz" )
      .Build();
  }

  /// <summary>
  /// Guest network isolation with firewall.
  /// Guest devices cannot see each other (client isolation), but can reach external subnet.
  /// Tests device-level isolation on same subnet.
  /// </summary>
  public static NetworkTopology GuestNetworkIsolation() {
    return new NetworkTopologyBuilder()
      .AddSubnet( "guest", "192.168.100.0/24", [
        ( "192.168.100.10", "Guest device 1" ),
        ( "192.168.100.11", "Guest device 2" ),
        ( "192.168.100.12", "Guest device 3" )
      ], out var guestSubnet )
      .AddSubnet( "external", "203.0.113.0/24", [
        ( "203.0.113.1", "Internet gateway" )
      ] )
      .AddAgent( new AgentId( "agentid_guest" ), guestSubnet )
      .WithFirewall( fw => {
        fw.Deny( FirewallTarget.Subnet( "guest" ), FirewallTarget.Subnet( "guest" ) ); // Block guest-to-guest
        fw.Allow( FirewallTarget.Subnet( "guest" ), FirewallTarget.Subnet( "external" ) ); // Allow guest-to-external
      } )
      .Build();
  }

  /// <summary>
  /// Bastion host scenario with device-level firewall rules.
  /// Only specific bastion host (192.168.1.10) can reach internal network.
  /// Other DMZ devices are blocked.
  /// Tests device-specific firewall rules with first-match-wins priority.
  /// </summary>
  public static NetworkTopology BastionHostAccess() {
    return new NetworkTopologyBuilder()
      .AddSubnet( "dmz", "192.168.1.0/24", [
        ( "192.168.1.10", "Bastion host" ),
        ( "192.168.1.20", "Web server" ),
        ( "192.168.1.30", "App server" )
      ], out var dmzSubnet )
      .AddSubnet( "internal", "10.0.0.0/24", [
        ( "10.0.0.100", "Database" ),
        ( "10.0.0.101", "Admin server" )
      ], out var internalSubnet )
      .AddAgent( new AgentId( "agentid_dmz" ), dmzSubnet )
      .AddAgent( new AgentId( "agentid_internal" ), internalSubnet )
      .WithFirewall( fw => {
        // Order matters - first match wins!
        fw.Allow( FirewallTarget.FromIp( new IpV4Address( "192.168.1.10" ) ),
          FirewallTarget.Subnet( "internal" ) ); // Bastion → internal (FIRST)
        fw.Deny( FirewallTarget.Subnet( "dmz" ), FirewallTarget.Subnet( "internal" ) ); // Other DMZ → internal (SECOND)
        fw.Allow( FirewallTarget.Subnet( "dmz" ), FirewallTarget.Subnet( "dmz" ) ); // DMZ can see itself
        fw.Allow( FirewallTarget.Subnet( "internal" ), FirewallTarget.Subnet( "internal" ) ); // Internal can see itself
        fw.Allow( FirewallTarget.Subnet( "internal" ), FirewallTarget.Subnet( "dmz" ) ); // Internal → DMZ allowed
      } )
      .Build();
  }

  /// <summary>
  /// Management network with full visibility using CIDR rules.
  /// Management subnet (10.0.0.0/24) can see everything.
  /// Production subnets are isolated from each other.
  /// Tests CIDR-based firewall rules.
  /// </summary>
  public static NetworkTopology ManagementNetworkWithCidr() {
    return new NetworkTopologyBuilder()
      .AddSubnet( "management", "10.0.0.0/24", [
        ( "10.0.0.10", "Admin workstation" )
      ], out var mgmtSubnet )
      .AddSubnet( "production1", "192.168.1.0/24", [
        ( "192.168.1.100", "Prod server 1" )
      ], out var prod1Subnet )
      .AddSubnet( "production2", "192.168.2.0/24", [
        ( "192.168.2.100", "Prod server 2" )
      ], out var prod2Subnet )
      .AddAgent( new AgentId( "agentid_mgmt" ), mgmtSubnet )
      .AddAgent( new AgentId( "agentid_prod1" ), prod1Subnet )
      .AddAgent( new AgentId( "agentid_prod2" ), prod2Subnet )
      .WithFirewall( fw => {
        // Management can reach everything
        fw.Allow( FirewallTarget.Subnet( "management" ), FirewallTarget.Any );

        // Production subnets can only see themselves
        fw.Allow( FirewallTarget.Subnet( "production1" ), FirewallTarget.Subnet( "production1" ) );
        fw.Allow( FirewallTarget.Subnet( "production2" ), FirewallTarget.Subnet( "production2" ) );

        // Block production cross-talk (explicit deny)
        fw.Deny( FirewallTarget.FromCidr( new CidrBlock( "192.168.0.0/16" ) ),
          FirewallTarget.FromCidr( new CidrBlock( "192.168.0.0/16" ) ) ); // Block any 192.168.x.x to 192.168.x.x
      } )
      .Build();
  }
}