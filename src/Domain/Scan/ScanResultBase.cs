namespace Drift.Domain.Scan;

public class ScanResultBase {
  public required Metadata Metadata {
    get;
    init;
  }

  public required ScanResultStatus Status {
    get;
    init;
  }

  public Percentage Progress {
    get;
    init;
  } = new(0);
}