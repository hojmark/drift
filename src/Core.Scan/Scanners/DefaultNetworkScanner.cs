using System.Globalization;
using Drift.Domain;
using Drift.Domain.Progress;
using Drift.Domain.Scan;
using Microsoft.Extensions.Logging;

namespace Drift.Core.Scan.Scanners;

public class DefaultNetworkScanner( ISubnetScannerProvider subnetScannerProvider ) : INetworkScanner {
  public event EventHandler<NetworkScanResult>? ResultUpdated;

  public async Task<NetworkScanResult> ScanAsync(
    NetworkScanOptions request,
    ILogger? logger = null,
    CancellationToken cancellationToken = default
  ) {
    var startedAt = DateTime.Now;
    logger?.LogDebug( "Starting network scan at {StartedAt}", startedAt.ToString( CultureInfo.InvariantCulture ) );

    EventHandler<SubnetScanResult> eventHandler = ( ( _, result ) => ResultUpdated?.Invoke( null,
      new NetworkScanResult {
        Metadata = new Metadata { StartedAt = startedAt },
        Status = ScanResultStatus.InProgress,
        Progress = result.Progress, //TODO not right
        Subnets = [result]
      } ) );

    var scanners = CreateScanners( request ); // TODO create scanner tasks that encapsulates logic better

    try {
      foreach ( var (_, scanner) in scanners ) {
        scanner.ResultUpdated += eventHandler;
      }

      var scannerTasks = scanners.Select( pair =>
        pair.Scanner.ScanAsync( new SubnetScanOptions { Cidr = pair.Cidr }, logger, cancellationToken )
      ).ToList();

      await Task.WhenAll( scannerTasks );

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
        Subnets = scannerTasks.Select( t => t.Result )
      };
    }
    finally {
      foreach ( var (_, scanner) in scanners ) {
        scanner.ResultUpdated -= eventHandler;
      }
    }
  }

  private List<(CidrBlock Cidr, ISubnetScanner Scanner)> CreateScanners( NetworkScanOptions options ) {
    return options.Cidrs
      .Select( cidr => ( Cidr: cidr, Scanner: subnetScannerProvider.GetScanner( cidr ) ) )
      .ToList();
  }
}