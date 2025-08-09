using Drift.Domain.Progress;

namespace Drift.Domain.Scan;

//TODO belongs to domain?
public interface INetworkScanner {
  public Task<ScanResult> ScanAsync(
    List<CidrBlock> cidrs,
    Action<ProgressReport>? onProgress = null,
    CancellationToken cancellationToken = default,
    int maxPingsPerSecond = 50
  );
}