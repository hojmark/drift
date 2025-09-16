namespace Drift.Domain.Scan;

public class Metadata {
  /// <summary>
  /// Local time
  /// </summary>
  public required DateTime StartedAt {
    get;
    init;
  }

  /// <summary>
  /// Local time
  /// </summary>
  public DateTime? EndedAt {
    get;
    init;
  }
}