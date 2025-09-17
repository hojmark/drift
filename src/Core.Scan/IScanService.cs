using Drift.Core.Scan.Model;
using Drift.Domain;
using Drift.Domain.NeoProgress;
using Microsoft.Extensions.Logging;

namespace Drift.Core.Scan;

public interface IScanService {
  Task<ScanResponse> ScanAsync(
    ScanRequest request,
    Action<ProgressNode>? onProgress = null,
    ILogger logger = null,
    CancellationToken cancellationToken = default
  );
}

public class ScanRequest {
  public Network? Spec {
    //Maybe just subnet or cidr+map function
    get;
    set;
  }

  // TODO Add enum for subnets: onlydeclared, includediscovered, all ?

  public int? MaxPingsPerSecond {
    get;
    set;
  } = 50;
}

public class ScanResponse {
  public ScanResult Result {
    get;
    set;
  }
}