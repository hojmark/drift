using Drift.Domain;
using Drift.Domain.Scan;
using Drift.Scanning.Scanners.Factories;

namespace Drift.Scanning.Tests.Utils;

internal sealed class PredefinedSubnetScannerFactory( ISubnetScanner subnetScanner ) : ISubnetScannerFactory {
  public ISubnetScanner Get( CidrBlock cidr ) {
    return subnetScanner;
  }
}