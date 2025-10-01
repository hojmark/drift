namespace Drift.Domain.Scan;

public class NetworkScanResult : ScanResultBase {
  public IReadOnlyCollection<SubnetScanResult> Subnets {
    get;
    init;
  } = [];

}