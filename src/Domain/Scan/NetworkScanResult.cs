namespace Drift.Domain.Scan;

public class NetworkScanResult {
  public required Metadata Metadata {
    get;
    init;
  }

  public required ScanResultStatus Status {
    get;
    init;
  }

  public IEnumerable<SubnetScanResult> Subnets {
    get;
    init;
  } = [];

  public Percentage Progress {
    get;
    init;
  } = new(0);
}