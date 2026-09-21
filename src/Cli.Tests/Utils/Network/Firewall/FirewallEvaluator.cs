using System.Net;
using Drift.Domain;
using Drift.Domain.Device.Addresses;

namespace Drift.Cli.Tests.Utils.Network.Firewall;

/// <summary>
/// Evaluates firewall rules with subnet awareness.
/// Combines firewall rules with subnet topology to determine if traffic is allowed.
/// </summary>
public sealed class FirewallEvaluator {
  private readonly FirewallRules _rules;
  private readonly IReadOnlyList<(string Name, CidrBlock Cidr)> _subnets;

  public FirewallEvaluator( FirewallRules rules, IReadOnlyList<(string Name, CidrBlock Cidr)> subnets ) {
    _rules = rules;
    _subnets = subnets;
  }

  /// <summary>
  /// Evaluate if traffic is allowed from source to destination.
  /// Rules are evaluated in order - first match wins.
  /// If no rule matches, the default policy from <see cref="FirewallRules.DefaultPolicy"/> is applied.
  /// </summary>
  /// <param name="source">Source firewall target (subnet, IP, CIDR, or wildcard).</param>
  /// <param name="dest">Destination firewall target (subnet, IP, CIDR, or wildcard).</param>
  /// <returns>True if traffic is allowed, false if denied.</returns>
  public bool IsAllowed( FirewallTarget source, FirewallTarget dest ) {
    // Extract subnet name and IP from source and destination targets
    var (sourceSubnet, sourceIp) = ResolveTarget( source );
    var (destSubnet, destIp) = ResolveTarget( dest );

    // Evaluate rules in order - first match wins
    foreach ( var rule in _rules.Rules ) {
      if ( RuleMatches( rule, sourceSubnet, destSubnet, sourceIp, destIp ) ) {
        return rule.Action == FirewallAction.Allow;
      }
    }

    // Default policy: defer to FirewallRules
    return _rules.DefaultPolicy == FirewallAction.Allow;
  }

  /// <summary>
  /// Resolve a FirewallTarget to a subnet name and optional IP address.
  /// For IP targets, looks up which subnet the IP belongs to.
  /// </summary>
  private (string? subnetName, IpV4Address? ip) ResolveTarget( FirewallTarget target ) {
    return target switch {
      FirewallTarget.SubnetTarget subnet => ( subnet.Name, null ),
      FirewallTarget.IpTarget ipTarget => ( ResolveSubnet( ipTarget.Address ), ipTarget.Address ),
      FirewallTarget.CidrTarget => ( null, null ), // CIDR targets don't resolve to a single subnet
      FirewallTarget.WildcardTarget => ( null, null ),
      _ => throw new InvalidOperationException( $"Unknown FirewallTarget type: {target.GetType().Name}" )
    };
  }

  /// <summary>
  /// Resolve an IP address to its subnet name by checking which subnet's CIDR contains the IP.
  /// </summary>
  private string? ResolveSubnet( IpV4Address ip ) {
    foreach ( var subnet in _subnets ) {
      if ( IpInCidr( ip, subnet.Cidr ) ) {
        return subnet.Name;
      }
    }

    return null; // IP doesn't belong to any known subnet
  }

  /// <summary>
  /// Check if an IP address is within a CIDR block.
  /// </summary>
  private static bool IpInCidr( IpV4Address ip, CidrBlock cidr ) {
    try {
      var ipAddress = IPAddress.Parse( ip.Value );
      return IPNetwork2.Parse( cidr.ToString() ).Contains( ipAddress );
    }
    catch {
      return false;
    }
  }

  /// <summary>
  /// Check if a firewall rule matches the given traffic parameters.
  /// </summary>
  private static bool RuleMatches(
    FirewallRule rule,
    string? sourceSubnet,
    string? destSubnet,
    IpV4Address? sourceIp,
    IpV4Address? destIp
  ) {
    bool sourceMatches = rule.Source.Matches( sourceSubnet ?? string.Empty, sourceIp );
    bool destMatches = rule.Destination.Matches( destSubnet ?? string.Empty, destIp );

    return sourceMatches && destMatches;
  }
}