using Drift.Domain;
using Drift.Domain.Device.Addresses;
using Drift.Domain.Scan;
using Drift.Scanning.Scanners;
using Microsoft.Extensions.Logging;

namespace Drift.Cli.Tests.Utils.Testing;

/// <summary>
/// Mock subnet scanner that returns predefined results for testing.
/// </summary>
public sealed class MockSubnetScanner( SubnetScanResult result ) : ISubnetScanner {
  public event EventHandler<SubnetScanResult>? ResultUpdated;

  public Task<SubnetScanResult> ScanAsync(
    SubnetScanOptions options,
    ILogger logger,
    CancellationToken cancellationToken = default
  ) {
    // Simulate progress updates
    var progressResult = new SubnetScanResult {
      CidrBlock = result.CidrBlock,
      DiscoveredDevices = result.DiscoveredDevices,
      Metadata = result.Metadata,
      Status = result.Status,
      DiscoveryAttempts = result.DiscoveryAttempts,
      Progress = new Percentage( 50 )
    };
    ResultUpdated?.Invoke( this, progressResult );

    var finalResult = new SubnetScanResult {
      CidrBlock = result.CidrBlock,
      DiscoveredDevices = result.DiscoveredDevices,
      Metadata = result.Metadata,
      Status = result.Status,
      DiscoveryAttempts = result.DiscoveryAttempts,
      Progress = new Percentage( 100 )
    };
    ResultUpdated?.Invoke( this, finalResult );

    return Task.FromResult( finalResult );
  }
}

/// <summary>
/// Mock factory that creates scanners with predefined results based on CIDR.
/// </summary>
public sealed class MockSubnetScannerFactory(
  Dictionary<CidrBlock, SubnetScanResult> resultsByCidr
) : ISubnetScannerFactory {
  public ISubnetScanner Get( CidrBlock cidr ) {
    if ( resultsByCidr.TryGetValue( cidr, out var result ) ) {
      return new MockSubnetScanner( result );
    }

    // Return empty result for unknown CIDRs
    return new MockSubnetScanner( new SubnetScanResult {
      CidrBlock = cidr,
      DiscoveredDevices = [],
      Metadata = new Metadata { StartedAt = default, EndedAt = default },
      Status = ScanResultStatus.Success,
      DiscoveryAttempts = System.Collections.Immutable.ImmutableHashSet<IpV4Address>.Empty
    } );
  }
}