using Drift.Domain;
using Drift.Domain.Scan;
using Microsoft.Extensions.Logging;

namespace Drift.Scanning.Tests.Utils;

#pragma warning disable CA1515 //TODO try to remove
public sealed class PredefinedScanOrchestrator( NetworkScanResult scanResult ) : IScanOrchestrator {
  public event EventHandler<NetworkScanResult>? ResultUpdated;

#pragma warning restore CA1515
  public Task<NetworkScanResult> ScanAsync(
    NetworkScanOptions options,
    ILogger logger,
    CancellationToken cancellationToken
  ) {
    ResultUpdated?.Invoke(
      this,
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
}