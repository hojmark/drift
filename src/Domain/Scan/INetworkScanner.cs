using Drift.Domain.Device.Discovered;
using Drift.Domain.Progress;
using Microsoft.Extensions.Logging;

namespace Drift.Domain.Scan;

public interface INetworkScanner {
  Task<NetworkScanResult> ScanAsync(
    NetworkScanOptions request,
    ILogger? logger = null,
    CancellationToken cancellationToken = default
  );

  event EventHandler<NetworkScanResult>? ResultUpdated;
}