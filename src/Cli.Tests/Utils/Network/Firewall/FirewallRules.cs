namespace Drift.Cli.Tests.Utils.Network.Firewall;

/// <summary>
/// Represents firewall rules for controlling network visibility in test topologies.
/// Rules are evaluated in order (first match wins), with a default DENY policy.
/// Use <see cref="FirewallEvaluator"/> to evaluate rules with subnet awareness.
/// </summary>
public sealed class FirewallRules {
  private readonly List<FirewallRule> _rules = new();

  /// <summary>
  /// Add an ALLOW rule. Rules are evaluated in the order they are added.
  /// </summary>
  /// <param name="source">Source target (subnet, IP, CIDR, or wildcard).</param>
  /// <param name="destination">Destination target (subnet, IP, CIDR, or wildcard).</param>
  public void Allow( FirewallTarget source, FirewallTarget destination ) {
    _rules.Add( new FirewallRule { Source = source, Destination = destination, Action = FirewallAction.Allow } );
  }

  /// <summary>
  /// Add a DENY rule. Rules are evaluated in the order they are added.
  /// </summary>
  /// <param name="source">Source target (subnet, IP, CIDR, or wildcard).</param>
  /// <param name="destination">Destination target (subnet, IP, CIDR, or wildcard).</param>
  public void Deny( FirewallTarget source, FirewallTarget destination ) {
    _rules.Add( new FirewallRule { Source = source, Destination = destination, Action = FirewallAction.Deny } );
  }

  /// <summary>
  /// Gets or sets the policy applied when no rule matches. Defaults to <see cref="FirewallAction.Allow"/>.
  /// </summary>
  public FirewallAction DefaultPolicy {
    get;
    set;
  } = FirewallAction.Allow; // TODO change to Deny

  public IReadOnlyList<FirewallRule> Rules => _rules.AsReadOnly();
}

/// <summary>
/// Represents a single firewall rule.
/// </summary>
public sealed class FirewallRule {
  /// <summary>
  /// Gets source firewall target (subnet, IP, CIDR, or wildcard).
  /// </summary>
  public required FirewallTarget Source {
    get;
    init;
  }

  /// <summary>
  /// Gets destination firewall target (subnet, IP, CIDR, or wildcard).
  /// </summary>
  public required FirewallTarget Destination {
    get;
    init;
  }

  /// <summary>
  /// Gets action to take when this rule matches.
  /// </summary>
  public required FirewallAction Action {
    get;
    init;
  }

  public override string ToString() =>
    $"{Action.ToString().ToUpper()}: {Source} → {Destination}";
}

/// <summary>
/// Action to take when a firewall rule matches.
/// </summary>
public enum FirewallAction {
  /// <summary>
  /// Allow the traffic to pass.
  /// </summary>
  Allow,

  /// <summary>
  /// Deny/block the traffic.
  /// </summary>
  Deny
}