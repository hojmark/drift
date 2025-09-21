using Drift.Domain;
using Drift.Domain.Scan;

namespace Drift.Core.Scan.Scanners;

public interface ISubnetScannerProvider {
  ISubnetScanner GetScanner( CidrBlock cidr );
}