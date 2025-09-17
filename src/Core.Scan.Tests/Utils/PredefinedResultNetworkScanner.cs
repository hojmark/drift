using Drift.Domain.Progress;
using Drift.Domain.Scan;
using Microsoft.Extensions.Logging;

namespace Drift.Core.Scan.Tests.Utils;

public class PredefinedResultNetworkScanner( NetworkScanResult scanResult ) : INetworkScanner {
  public Task<NetworkScanResult> ScanAsync(
    NetworkScanOptions request,
    ILogger? logger = null,
    CancellationToken cancellationToken = default
  ) {
    var intermediateResult = new NetworkScanResult { Metadata = scanResult.Metadata, Status = ScanResultStatus.InProgress };
    ResultUpdated?.Invoke( this, intermediateResult );
    return Task.FromResult( scanResult );
  }

  public Task<NetworkScanResult> ScanAsyncOld(
    NetworkScanOptions request,
    ILogger? logger = null,
    Action<ProgressReport>? onProgress = null,
    CancellationToken cancellationToken = default
  ) {
    return ScanAsync( request, cancellationToken: cancellationToken );
  }

  // TODO call this before the scan is done i.e. in progress
  public event EventHandler<NetworkScanResult>? ResultUpdated;
}