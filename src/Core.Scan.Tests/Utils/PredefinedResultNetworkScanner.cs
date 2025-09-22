using Drift.Domain;
using Drift.Domain.Progress;
using Drift.Domain.Scan;
using Microsoft.Extensions.Logging;

namespace Drift.Core.Scan.Tests.Utils;

#pragma warning disable CA1515 //TODO try to remove
public sealed class PredefinedResultNetworkScanner( NetworkScanResult scanResult ) : INetworkScanner {
#pragma warning restore CA1515
  public Task<NetworkScanResult> ScanAsync(
    NetworkScanOptions request,
    ILogger? logger = null,
    CancellationToken cancellationToken = default
  ) {
    ResultUpdated?.Invoke( this,
      new NetworkScanResult {
        Metadata = scanResult.Metadata,
        Status = ScanResultStatus.InProgress,
        Progress = Percentage.Zero,
        Subnets = scanResult.Subnets
      } );

    var finalResult = new NetworkScanResult {
      Metadata = scanResult.Metadata,
      Status = ScanResultStatus.Success,
      Progress = Percentage.Hundred,
      Subnets = scanResult.Subnets
    };

    ResultUpdated?.Invoke( this, finalResult );

    return Task.FromResult( finalResult );
  }

  // TODO call this before the scan is done i.e. in progress
  public event EventHandler<NetworkScanResult>? ResultUpdated;
}