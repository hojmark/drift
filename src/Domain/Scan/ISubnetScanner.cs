using Microsoft.Extensions.Logging;

namespace Drift.Domain.Scan;

public interface ISubnetScanner {
  Task<SubnetScanResult> ScanAsync(
    SubnetScanOptions options,
    ILogger? logger = null,
    CancellationToken cancellationToken = default
  );

  event EventHandler<SubnetScanResult>? ResultUpdated;
}