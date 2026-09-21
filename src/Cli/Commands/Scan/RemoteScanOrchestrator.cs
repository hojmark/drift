using Drift.Coordinator.Client;
using Drift.Domain.Scan;
using Microsoft.Extensions.Logging;

namespace Drift.Cli.Commands.Scan;

/// <summary>
/// Adapts the coordinator's Control API to the local scan rendering pipeline.
/// </summary>
internal sealed class RemoteScanOrchestrator(
  ControlApiClient client,
  ILogger coordinatorLogger
) : IScanOrchestrator, IDisposable {
  public event EventHandler<NetworkScanResult>? ResultUpdated;

  public async Task<NetworkScanResult> ScanAsync(
    NetworkScanOptions options,
    ILogger logger,
    CancellationToken cancellationToken
  ) {
    logger.LogDebug( "Starting coordinator scan" );
    var scanId = await client.StartScanAsync( options.PingsPerSecond, cancellationToken );
    coordinatorLogger.LogInformation( "Coordinator scan {ScanId} started", scanId );

    NetworkScanResult? latest = null;
    try {
      await foreach ( var result in client.WatchScanAsync( scanId, cancellationToken ) ) {
        latest = result;
        ResultUpdated?.Invoke( this, result );
      }
    }
    catch ( HttpRequestException exception ) {
      coordinatorLogger.LogWarning(
        exception,
        "Coordinator scan event stream interrupted; trying the result endpoint"
      );
      latest = await client.GetResultAsync( scanId, cancellationToken );
      ResultUpdated?.Invoke( this, latest );
    }

    if ( latest == null ) {
      latest = await client.GetResultAsync( scanId, cancellationToken );
      ResultUpdated?.Invoke( this, latest );
    }

    logger.LogDebug( "Coordinator scan {ScanId} completed", scanId );
    return latest;
  }

  public void Dispose() {
    client.Dispose();
  }
}