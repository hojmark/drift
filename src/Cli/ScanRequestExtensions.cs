using Drift.Common;
using Drift.Domain;
using Drift.Domain.Scan;

namespace Drift.Cli;

internal static class ScanRequestExtensions {
  internal static TimeSpan EstimatedDuration( this NetworkScanOptions scanRequest ) {
    return scanRequest.Cidrs.Aggregate( TimeSpan.Zero,
      ( current, cidr ) => current + EstimatedDuration( scanRequest, cidr )
    );
  }

  internal static TimeSpan EstimatedDuration( this NetworkScanOptions scanRequest, CidrBlock cidr ) {
    if ( !scanRequest.Cidrs.Contains( cidr ) ) {
      throw new ArgumentException( "CIDR block not found in scan request", nameof(cidr) );
    }

    double hostCount = IpNetworkUtils.GetIpRangeCount( cidr );
    double totalSeconds = hostCount / scanRequest.PingsPerSecond;
    return TimeSpan.FromSeconds( totalSeconds );
  }
}