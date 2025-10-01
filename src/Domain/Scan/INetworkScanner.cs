using Microsoft.Extensions.Logging;

namespace Drift.Domain.Scan;

//TODO belongs to domain?
public interface INetworkScanner {
  Task<NetworkScanResult> ScanAsync(
    NetworkScanOptions options,
    ILogger? logger = null,
    CancellationToken cancellationToken = default
  );

  event EventHandler<NetworkScanResult>? ResultUpdated;
}