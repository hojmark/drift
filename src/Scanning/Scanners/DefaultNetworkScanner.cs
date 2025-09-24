using System.Collections.Concurrent;
using System.Globalization;
using Drift.Domain;
using Drift.Domain.Scan;
using Microsoft.Extensions.Logging;

namespace Drift.Scanning.Scanners;

public class DefaultNetworkScanner( ISubnetScannerFactory subnetScannerFactory ) : INetworkScanner {
  public event EventHandler<NetworkScanResult>? ResultUpdated;

  public async Task<NetworkScanResult> ScanAsync(
    NetworkScanOptions options,
    ILogger logger,
    CancellationToken cancellationToken = default
  ) {
    var startedAt = DateTime.Now;

    logger.LogDebug( "Starting network scan at {StartedAt}", startedAt.ToString( CultureInfo.InvariantCulture ) );

    var subnetScanners = CreateScanners( options ); // TODO create scanner tasks that encapsulates logic better
    var subnetIntermediateResults = new ConcurrentDictionary<CidrBlock, SubnetScanResult>();
    var subnetScanTasks = new List<Task<SubnetScanResult>>();

    foreach ( var (cidr, scanner) in subnetScanners ) {
      var task = RunScanAsync(
        cidr,
        scanner,
        options.PingsPerSecond,
        result => {
          subnetIntermediateResults[cidr] = result;
          UpdateProgress( startedAt, subnetIntermediateResults.Values );
        },
        logger,
        cancellationToken
      );

      subnetScanTasks.Add( task );
    }

    await Task.WhenAll( subnetScanTasks );

    var finishedAt = DateTime.Now;
    var elapsed =
      finishedAt - startedAt; // TODO .Humanize( 2, CultureInfo.InvariantCulture, minUnit: TimeUnit.Second )

    var result = new NetworkScanResult {
      Metadata = new Metadata { StartedAt = startedAt, EndedAt = finishedAt },
      Status = ScanResultStatus.Success,
      Progress = Percentage.Hundred,
      Subnets = subnetScanTasks.Select( t => t.Result ).ToList()
    };

    ResultUpdated?.Invoke( this, result );

    logger?.LogDebug(
      "Finished network scan at {StartedAt} in {Elapsed}",
      finishedAt.ToString( CultureInfo.InvariantCulture ),
      elapsed
    );

    return result;
  }

  private static async Task<SubnetScanResult> RunScanAsync(
    CidrBlock cidr,
    ISubnetScanner scanner,
    uint pingsPerSecond,
    Action<SubnetScanResult> onProgress,
    ILogger logger,
    CancellationToken cancellationToken
  ) {
    void Handler( object? sender, SubnetScanResult result ) => onProgress( result );
    scanner.ResultUpdated += Handler;

    try {
      return await scanner.ScanAsync(
        new SubnetScanOptions {
          Cidr = cidr,
          // TODO not the right value
          PingsPerSecond = pingsPerSecond
        },
        logger,
        cancellationToken
      );
    }
    finally {
      scanner.ResultUpdated -= Handler;
    }
  }

  private void UpdateProgress(
    DateTime startedAt,
    ICollection<SubnetScanResult> subnetResults
  ) {
    if ( ResultUpdated == null || !subnetResults.Any() ) {
      return;
    }

    var aggregated = new Percentage( (byte) subnetResults.Average( r => r.Progress.Value ) );

    var inProgressResult = new NetworkScanResult {
      Metadata = new Metadata { StartedAt = startedAt },
      Status = ScanResultStatus.InProgress,
      Progress = aggregated,
      Subnets = subnetResults.ToList()
    };

    ResultUpdated?.Invoke( this, inProgressResult );
  }

  private List<(CidrBlock Cidr, ISubnetScanner Scanner)> CreateScanners( NetworkScanOptions options ) {
    return options.Cidrs
      .Select( cidr => ( Cidr: cidr, Scanner: subnetScannerFactory.Get( cidr ) ) )
      .ToList();
  }
}