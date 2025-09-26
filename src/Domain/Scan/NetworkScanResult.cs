namespace Drift.Domain.Scan;

public class NetworkScanResult : ScanResultBase {
  public IEnumerable<SubnetScanResult> Subnets {
    get;
    init;
  } = [];

}