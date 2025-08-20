using Drift.Core.Scan;
using Drift.Core.Scan.Model;
using Drift.Domain;
using Drift.Domain.NeoProgress;
using Drift.Domain.Progress;

namespace Drift.Core.Abstractions;

public interface IScanService {
  Task<ScanResponse> ScanAsync( ScanRequest request,
    Action<ProgressNodeNew>? onProgress = null,
    CancellationToken cancellationToken = default );
}

public class ScanRequest {
  public Network? Spec { //Maybe just subnet or cidr+map function
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