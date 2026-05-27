using Drift.Cli.Tests.Utils.Network.Firewall;
using Drift.Domain;
using Drift.Domain.Device.Addresses;

namespace Drift.Cli.Tests.Utils.Network.Topology;

public sealed class NetworkTopologyBuilder {
  private readonly Dictionary<string, NetworkTopology.SubnetDefinition> _subnets = new();
  private readonly List<NetworkTopology.AgentDefinition> _agents = [];
  private NetworkTopology.SubnetDefinition? _cli;
  private FirewallRules? _firewall;

  public NetworkTopologyBuilder AddSubnet(
    string name,
    string cidr,
    List<(string ip, string description)>? devices,
    out NetworkTopology.SubnetDefinition subnet
  ) {
    subnet = new NetworkTopology.SubnetDefinition {
      Name = name,
      Cidr = new CidrBlock( cidr ),
      Devices = devices?.Select( d => new NetworkTopology.DeviceDefinition {
        Ip = new IpV4Address( d.ip ), Mac = GenerateMacFromIp( d.ip ), Description = d.description
      } ).ToList() ?? []
    };

    _subnets[name] = subnet;
    return this;
  }

  public NetworkTopologyBuilder AddSubnet(
    string name,
    string cidr,
    List<(string ip, string description)>? devices = null
  ) => AddSubnet( name, cidr, devices, out _ );

  /// <summary>
  /// Attach the CLI to a specific subnet.
  /// </summary>
  public NetworkTopologyBuilder WithCli( NetworkTopology.SubnetDefinition subnet ) {
    _cli = subnet;
    return this;
  }

  /// <summary>
  /// Add an agent attached to specific subnets.
  /// </summary>
  public NetworkTopologyBuilder AddAgent( AgentId id, NetworkTopology.SubnetDefinition attachedSubnet) {
    _agents.Add( new NetworkTopology.AgentDefinition { Id = id, AttachedSubnet = attachedSubnet } );
    return this;
  }

  /// <summary>
  /// Configure firewall rules using a configuration action.
  /// When firewall is configured, rules are evaluated in order with default ALLOW policy.
  /// </summary>
  public NetworkTopologyBuilder WithFirewall( Action<FirewallRules> configure ) {
    _firewall ??= new FirewallRules();
    configure( _firewall );
    return this;
  }

  /// <summary>
  /// Shorthand: Add an ALLOW rule from source to destination.
  /// Creates a firewall if one doesn't exist yet.
  /// </summary>
  public NetworkTopologyBuilder AllowConnection( string source, string destination ) {
    _firewall ??= new FirewallRules();
    _firewall.Allow( FirewallTarget.Subnet( source ), FirewallTarget.Subnet( destination ) );
    return this;
  }

  /// <summary>
  /// Shorthand: Add a DENY rule from source to destination.
  /// Creates a firewall if one doesn't exist yet.
  /// </summary>
  public NetworkTopologyBuilder DenyConnection( string source, string destination ) {
    _firewall ??= new FirewallRules();
    _firewall.Deny( FirewallTarget.Subnet( source ), FirewallTarget.Subnet( destination ) );
    return this;
  }

  /// <summary>
  /// Build the topology.
  /// </summary>
  public NetworkTopology Build() {
    var subnets = _subnets.Values.ToList();

    // Construct FirewallEvaluator if firewall rules exist
    FirewallEvaluator? evaluator = null;
    if ( _firewall != null ) {
      evaluator = new FirewallEvaluator( _firewall, subnets.Select( s => ( s.Name, s.Cidr ) ).ToList() );
    }

    return new NetworkTopology {
      Subnets = subnets,
      Cli = new NetworkTopology.CliDefinition { AttachedSubnet = _cli },
      Agents = _agents,
      FirewallRules = _firewall,
      FirewallEvaluator = evaluator
    };
  }

  private static MacAddress GenerateMacFromIp( string ip ) {
    var parts = ip.Split( '.' );
    var macString =
      $"aa:bb:{int.Parse( parts[0] ):x2}:{int.Parse( parts[1] ):x2}:{int.Parse( parts[2] ):x2}:{int.Parse( parts[3] ):x2}";
    return new MacAddress( macString );
  }
}