namespace Drift.Domain.Scan;

public class Metadata {
  /// <summary>
  /// Gets the local time the action was started at.
  /// </summary>
  public required DateTime StartedAt {
    get;
    init;
  }

  /// <summary>
  /// Gets the local time the action was ended at.
  /// </summary>
  public DateTime? EndedAt {
    get;
    init;
  }
}