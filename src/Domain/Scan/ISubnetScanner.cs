using Microsoft.Extensions.Logging;

namespace Drift.Domain.Scan;

public interface ISubnetScanner {
  event EventHandler<SubnetScanResult>? ResultUpdated;

  Task<SubnetScanResult> ScanAsync(
    SubnetScanOptions options,
    ILogger logger,
    CancellationToken cancellationToken = default
  );
}