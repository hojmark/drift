using Drift.Domain.Device.Discovered;
using Drift.Domain.Progress;
using Microsoft.Extensions.Logging;

namespace Drift.Domain.Scan;

public interface INetworkScanner {
  Task<NetworkScanResult> ScanAsync(
    NetworkScanOptions request,
    ILogger? logger = null,
    CancellationToken cancellationToken = default
  );

  event EventHandler<NetworkScanResult>? ResultUpdated;
  //event EventHandler<NetworkScanResult>? ResultUpdated;
  //event EventHandler<SubnetScanResult>? SubnetCompleted;
}

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

public interface ISubnetScanner {
  Task<SubnetScanResult> ScanAsync(
    SubnetScanOptions options,
    ILogger? logger = null,
    CancellationToken cancellationToken = default
  );

  event EventHandler<SubnetScanResult>? ResultUpdated;
}

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