using Drift.Domain;
using Drift.Domain.Progress;

namespace Drift.Core.Abstractions;

public interface IScanService {
  public Task<ScanResponse> ExecuteScanAsync(
    ScanRequest request,
    Action<ProgressReport>? onProgress,
    CancellationToken cancellationToken = default
  );
}

public class ScanRequest {
  public Network? Spec {
    get;
    set;
  }

  // TODO Add enum for subnets: onlydeclared, includediscovered, all ?

  public List<CidrBlock>? Subnets {
    get;
    set;
  }

  public int? MaxPingsPerSecond {
    get;
    set;
  } = 50;
}

public class ScanResponse {
}