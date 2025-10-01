using Drift.Domain;
using Drift.Domain.Scan;
using Drift.Scanning.Scanners;

namespace Drift.Scanning.Tests.Utils;

internal sealed class PredefinedSubnetScannerFactory( ISubnetScanner scanner ) : ISubnetScannerFactory {
  public ISubnetScanner Get( CidrBlock cidr ) {
    return scanner;
  }
}