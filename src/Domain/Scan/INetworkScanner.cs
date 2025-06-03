using Drift.Domain.Progress;

namespace Drift.Domain.Scan;

//TODO belongs to domain?
public interface INetworkScanner {
  public Task<ScanResult> ScanAsync(
    CidrBlock cidr,
    Action<ProgressReport>? onProgress = null,
    CancellationToken cancellationToken = default
  );
}