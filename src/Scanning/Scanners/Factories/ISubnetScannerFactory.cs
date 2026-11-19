using Drift.Domain;
using Drift.Domain.Scan;

namespace Drift.Scanning.Scanners.Factories;

public interface ISubnetScannerFactory {
  ISubnetScanner Get( CidrBlock cidr );
}