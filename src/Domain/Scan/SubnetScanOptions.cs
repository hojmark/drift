namespace Drift.Domain.Scan;

public class SubnetScanOptions {
  public required CidrBlock Cidr {
    get;
    init;
  }

  public uint PingsPerSecond {
    get;
    init;
  } = 50;
}