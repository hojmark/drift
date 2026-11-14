namespace Drift.Networking.Core.Abstractions;

/// <summary>
/// Indicates whether a connection was known to be closed locally.
/// </summary>
public enum ConnectionCloseOrigin {
  /// <summary>The connection was not known to be closed locally.</summary>
  Unknown,

  /// <summary>The connection was known to be closed locally.</summary>
  Local,
}
