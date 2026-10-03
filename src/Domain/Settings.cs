namespace Drift.Domain;

/// <summary>
/// Network-wide behavioral settings, distinct from the declared network topology itself.
/// </summary>
public record Settings {
  /// <summary>
  /// Gets or sets whether devices discovered on the network but not declared in the spec are
  /// considered a spec violation (<see cref="Domain.UnknownDevicePolicy.Disallowed"/>) or
  /// tolerated (<see cref="Domain.UnknownDevicePolicy.Allowed"/>).
  /// </summary>
  public UnknownDevicePolicy? UnknownDevices {
    get;
    set;
  }

  /// <summary>
  /// Gets or sets the default policy for connections that are not explicitly declared by any
  /// agent's policy block: whether they are expected to be
  /// <see cref="Domain.UndeclaredConnectionsPolicy.Reachable"/> or
  /// <see cref="Domain.UndeclaredConnectionsPolicy.Blocked"/> by default.
  /// </summary>
  public UndeclaredConnectionsPolicy? UndeclaredConnections {
    get;
    set;
  }

  public int? PingThrottling {
    get;
    set;
  }

  public bool? ScanOnlyDeclaredSubnets {
    get;
    set;
  }
}

/// <summary>
/// Governs whether a device discovered on the network but not declared in the spec is treated
/// as adhering to the spec or not.
/// </summary>
public enum UnknownDevicePolicy {
  Disallowed = 1,
  Allowed = 2
}

/// <summary>
/// Governs whether network connections that are not explicitly declared by any agent's policy
/// block are expected to be reachable or blocked by default.
/// </summary>
public enum UndeclaredConnectionsPolicy {
  Blocked = 1,
  Reachable = 2
}
