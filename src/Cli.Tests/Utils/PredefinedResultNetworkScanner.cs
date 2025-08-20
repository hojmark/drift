using Drift.Core.Scan;
using Drift.Core.Scan.Model;
using Drift.Domain;
using Drift.Domain.Progress;
using Microsoft.Extensions.Logging;

namespace Drift.Cli.Tests.Utils;

public class PredefinedResultNetworkScanner( ScanResult scanResult ) : INetworkScanner {
  public Task<ScanResult> ScanAsync(
    List<CidrBlock> cidrs,
    ILogger logger,
    Action<ProgressReport>? onProgress = null,
    CancellationToken cancellationToken = default,
    int maxPingsPerSecond = 50
  ) {
    return Task.FromResult( scanResult );
  }
}