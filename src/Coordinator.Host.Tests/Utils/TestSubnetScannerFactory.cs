using Drift.Domain;
using Drift.Domain.Scan;
using Drift.Scanning.Scanners.Factories;

namespace Drift.Coordinator.Host.Tests.Utils;

internal sealed class TestSubnetScannerFactory : ISubnetScannerFactory {
  public ISubnetScanner Get( CidrBlock cidr ) => new TestSubnetScanner();
}

internal sealed class TestSubnetScanner : ISubnetScanner {
  public event EventHandler<SubnetScanResult>? ResultUpdated;

  public Task<SubnetScanResult> ScanAsync(
    SubnetScanOptions options,
    Microsoft.Extensions.Logging.ILogger logger,
    CancellationToken cancellationToken = default
  ) {
    var result = new SubnetScanResult {
      CidrBlock = options.Cidr,
      Metadata = new Metadata { StartedAt = DateTime.UtcNow, EndedAt = DateTime.UtcNow },
      Status = ScanResultStatus.Success,
      Progress = Percentage.Hundred
    };
    ResultUpdated?.Invoke( this, result );
    return Task.FromResult( result );
  }
}