using Drift.Domain;
using Drift.Domain.Progress;
using Drift.Domain.Scan;

namespace Drift.Cli.Tests.Utils;

public class PredefinedResultNetworkScanner( ScanResult scanResult ) : INetworkScanner {
  public Task<ScanResult> ScanAsync(
    CidrBlock cidr,
    Action<ProgressReport>? onProgress = null,
    CancellationToken cancellationToken = default
  ) {
    return Task.FromResult( scanResult );
  }
}