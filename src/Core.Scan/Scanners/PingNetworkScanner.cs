using System.Globalization;
using Drift.Domain;
using Drift.Domain.Progress;
using Drift.Domain.Scan;
using Microsoft.Extensions.Logging;

namespace Drift.Core.Scan.Scanners;

public class PingNetworkScanner( IPingTool pingTool ) : INetworkScanner {
  public event EventHandler<NetworkScanResult>? ResultUpdated;

  public Task<NetworkScanResult> ScanAsync(
    NetworkScanOptions request,
    ILogger? logger = null,
    CancellationToken cancellationToken = default
  ) {
    return ScanAsyncOld( request, logger, null, cancellationToken );
  }

  public async Task<NetworkScanResult> ScanAsyncOld(
    NetworkScanOptions request,
    ILogger? logger = null,
    Action<ProgressReport>? onProgress = null,
    CancellationToken cancellationToken = default
  ) {
    var startedAt = DateTime.Now;
    logger?.LogDebug( "Starting network scan at {StartedAt}", startedAt.ToString( CultureInfo.InvariantCulture ) );

    EventHandler<SubnetScanResult> eventHandler = ( ( _, result ) => ResultUpdated?.Invoke( null,
      new NetworkScanResult {
        Metadata = null, Status = ScanResultStatus.InProgress, Subnets = [result], Progress = result.Progress
      } ) );

    var localSubnetScanner = new LocalSubnetScanner( pingTool );
    localSubnetScanner.ResultUpdated += eventHandler;

    try {
      var subnetScanners = request.Cidrs.Select( c =>
        localSubnetScanner.ScanAsync(
          new SubnetScanOptions { Cidr = c }, logger, onProgress, cancellationToken )
      ).ToList();

      await Task.WhenAll( subnetScanners );

      var finishedAt = DateTime.Now;
      var elapsed =
        finishedAt - startedAt; // TODO .Humanize( 2, CultureInfo.InvariantCulture, minUnit: TimeUnit.Second )
      logger?.LogDebug( "Finished network scan at {StartedAt} in {Elapsed}",
        finishedAt.ToString( CultureInfo.InvariantCulture ),
        elapsed
      );

      return new NetworkScanResult {
        Metadata = new Metadata { StartedAt = startedAt, EndedAt = DateTime.Now },
        Status = ScanResultStatus.Success,
        Progress = Percentage.Hundred,
        Subnets = subnetScanners.Select( t => t.Result )
      };
    }
    finally {
      localSubnetScanner.ResultUpdated -= eventHandler;
    }
  }
}