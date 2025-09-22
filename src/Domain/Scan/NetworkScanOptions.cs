namespace Drift.Domain.Scan;

public class NetworkScanOptions {
  public List<CidrBlock> Cidrs {
    get;
    init;
  } = [];

  public uint PingsPerSecond {
    get;
    init;
  } = 50;
}