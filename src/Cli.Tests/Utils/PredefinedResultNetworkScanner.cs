using Drift.Domain;
using Drift.Domain.Progress;
using Drift.Domain.Scan;

namespace Drift.Cli.Tests.Utils;

public class PredefinedResultNetworkScanner( ScanResult scanResult ) : INetworkScanner {
  public Task<ScanResult> ScanAsync(
    List<CidrBlock> cidrs,
    Action<ProgressReport>? onProgress = null,
    CancellationToken cancellationToken = default,
    int maxPingsPerSecond = 50
  ) {
    return Task.FromResult( scanResult );
  }

  public event EventHandler<ScanResult>? ResultUpdated;
}