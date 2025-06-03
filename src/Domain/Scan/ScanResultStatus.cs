namespace Drift.Domain.Scan;

public enum ScanResultStatus {
  /// <summary>
  /// Scan completed successfully
  /// </summary>
  Success = 1,

  /// <summary>
  /// Scan is currently running
  /// </summary>
  InProgress = 2,

  /// <summary>
  /// Scan was canceled before completion
  /// </summary>
  Canceled = 3,

  /// <summary>
  /// Scan could not complete due to an unexpected technical problem (exception, crash, etc.)
  /// </summary>
  Error = 4
}