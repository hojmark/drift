namespace Drift.Networking.Cluster;

public sealed class ClusterOptions {
  /// <summary>
  /// Gets the maximum number of retry attempts for failed operations.
  /// </summary>
  public int MaxRetryAttempts {
    get;
    init;
  } = 3;

  /// <summary>
  /// Gets the base delay between retry attempts in milliseconds.
  /// </summary>
  public int RetryBaseDelayMs {
    get;
    init;
  } = 100;

  /// <summary>
  /// Gets the maximum delay between retry attempts in milliseconds.
  /// </summary>
  public int RetryMaxDelayMs {
    get;
    init;
  } = 5000;

  /// <summary>
  /// Gets the default timeout for send-and-wait operations.
  /// </summary>
  public TimeSpan DefaultTimeout {
    get;
    init;
  } = TimeSpan.FromSeconds( 30 );

  /// <summary>
  /// Gets the default timeout for streaming operations (e.g., scans).
  /// </summary>
  public TimeSpan StreamingTimeout {
    get;
    init;
  } = TimeSpan.FromMinutes( 5 );
}