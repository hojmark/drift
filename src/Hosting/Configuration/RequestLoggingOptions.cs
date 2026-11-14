namespace Drift.Hosting.Configuration;

/// <summary>
/// Controls logging of incoming HTTP requests (including gRPC calls).
/// </summary>
public sealed class RequestLoggingOptions {
  /// <summary>
  /// Gets or sets a value indicating whether each request is logged.
  /// </summary>
  public bool Enabled {
    get;
    set;
  }
}