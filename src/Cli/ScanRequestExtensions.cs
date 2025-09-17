using Drift.Domain;
using Drift.Domain.Scan;
using Drift.Utils;

namespace Drift.Cli;

internal static class ScanRequestExtensions {
  internal static TimeSpan Duration( this ScanRequest scanRequest ) {
    return scanRequest.Cidrs.Aggregate( TimeSpan.Zero,
      ( current, cidr ) => current + Duration( scanRequest, cidr )
    );
  }

  internal static TimeSpan Duration( this ScanRequest scanRequest, CidrBlock cidr ) {
    if ( !scanRequest.Cidrs.Contains( cidr ) ) {
      throw new ArgumentException( "CIDR block not found in scan request", nameof(cidr) );
    }

    double hostCount = IpNetworkUtils.GetIpRangeCount( cidr );
    double totalSeconds = hostCount / scanRequest.PingsPerSecond;
    return TimeSpan.FromSeconds( totalSeconds );
  }
}