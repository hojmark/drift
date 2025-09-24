using Microsoft.Extensions.Logging;

namespace Drift.Domain.Scan;

// TODO belongs to domain?
public interface INetworkScanner {
  event EventHandler<NetworkScanResult>? ResultUpdated;

  Task<NetworkScanResult> ScanAsync(
    NetworkScanOptions options,
    ILogger logger,
    CancellationToken cancellationToken = default
  );
}