using Drift.Domain.Device.Discovered;
using Drift.Domain.Progress;
using Microsoft.Extensions.Logging;

namespace Drift.Domain.Scan;

//TODO belongs to domain?
/*[Obsolete( "Use IScanService instead" )]
public interface INetworkScanner {
  public Task<ScanResult> ScanAsync(
    ScanRequest request,
    Action<ProgressReport>? onProgress = null,
    CancellationToken cancellationToken = default
  );

  event EventHandler<ScanResult>? ResultUpdated;
}*/

#region NEO

public interface ISubnetScannerNEO {
  Task<SubnetScanResult> ScanAsync(
    CidrBlock subnet,
    SubnetScanOptions options,
    CancellationToken cancellationToken
  );

  event EventHandler<SubnetScanResult>? ResultUpdated;
}

// Multiple subnets (possibly distributed)
public interface INetworkScannerNEO {
  Task<NetworkScanResult> ScanAsync(
    List<CidrBlock> subnets,
    NetworkScanOptions options,
    CancellationToken cancellationToken
  );

  event EventHandler<NetworkScanResult>? ResultUpdated;
  event EventHandler<SubnetScanResult>? SubnetCompleted;
}

public class SubnetScanResult {
  public required ScanResultStatus Status {
    get;
    init;
  }


  public IEnumerable<DiscoveredDevice> DiscoveredDevices {
    get;
    init;
  } = [];
}

public class NetworkScanResult {
  public required ScanResultStatus Status {
    get;
    init;
  }


  public IEnumerable<DiscoveredSubnet> DiscoveredSubnets {
    get;
    init;
  } = [];
}

public class DiscoveredSubnet {
  public CidrBlock Address {
    get;
  }

  public List<DiscoveredDevice> Devices {
    get;
  }
}

public class NetworkScanOptions {
}

public class SubnetScanOptions {
}

#endregion

public interface IScanService {
  Task<ScanResult> ScanAsync(
    ScanRequest request,
    ILogger? logger = null,
    CancellationToken cancellationToken = default
  );

  [Obsolete( "Use other ScanAsync method plus ResultUpdated instead" )]
  Task<ScanResult> ScanAsyncOld(
    ScanRequest request,
    ILogger? logger = null,
    Action<ProgressReport>? onProgress = null,
    CancellationToken cancellationToken = default
  );

  event EventHandler<ScanResult>? ResultUpdated;
  //event EventHandler<ScanLogEventArgs>? MessageLogged;
}

public class ScanRequest {
  public List<CidrBlock> Cidrs {
    get;
    init;
  } = new();

  public uint PingsPerSecond {
    get;
    init;
  } = 50;
}

public abstract class ScanResultEventArgs : EventArgs {
  public ScanResult IntermediateResult {
    get;
    init;
  }
}

public abstract class ScanLogEventArgs : EventArgs {
  public string Message {
    get;
    init;
  }

  //public LogLevel Level { get; init; }
}