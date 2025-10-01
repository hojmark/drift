using Drift.Domain;
using Drift.Domain.Scan;

namespace Drift.Scanning.Scanners;

public interface ISubnetScannerFactory {
  ISubnetScanner Get( CidrBlock cidr );
}