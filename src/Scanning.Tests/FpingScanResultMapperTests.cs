using System.Net;
using Drift.Common;
using Drift.Domain;
using Drift.Domain.Device.Addresses;
using Drift.Domain.Device.Discovered;
using Drift.Domain.Scan;
using Drift.Scanning.Scanners;

namespace Drift.Scanning.Tests;

internal sealed class FpingScanResultMapperTests {
  private static readonly CidrBlock Cidr = new("192.0.2.0/30");
  private static readonly IPAddress[] IpRange = [IPAddress.Parse( "192.0.2.1" ), IPAddress.Parse( "192.0.2.2" )];

  private static readonly DiscoveredDevice[] DiscoveredDevices = [
    new() { Addresses = [new IpV4Address( "192.0.2.1" )] }
  ];

  [Test]
  public void Create_ExitCodeOne_MarksWholeRangeAttempted() {
    var result = CreateResult( exitCode: 1 );

    using ( Assert.EnterMultipleScope() ) {
      Assert.That( result.Status, Is.EqualTo( ScanResultStatus.Success ) );
      Assert.That( result.DiscoveryAttempts, Is.EquivalentTo( IpRange.Select( ip => new IpV4Address( ip ) ) ) );
      Assert.That( result.ErrorMessage, Is.Null );
    }
  }

  [Test]
  public void Create_UnexpectedExitCode_ReportsErrorAndOnlyKnownAttempts() {
    var result = CreateResult( exitCode: 2, standardError: "fping failed" );

    using ( Assert.EnterMultipleScope() ) {
      Assert.That( result.Status, Is.EqualTo( ScanResultStatus.Error ) );
      Assert.That(
        result.DiscoveryAttempts,
        Is.EquivalentTo( [new IpV4Address( "192.0.2.1" )] )
      );
      Assert.That( result.ErrorMessage, Is.EqualTo( "fping failed with exit code 2. fping failed" ) );
    }
  }

  private static SubnetScanResult CreateResult( int exitCode, string standardError = "" ) {
    return FpingScanResultMapper.Create(
      Cidr,
      DiscoveredDevices,
      new ToolResult( string.Empty, standardError, exitCode, false ),
      startedAt: DateTime.UnixEpoch,
      endedAt: DateTime.UnixEpoch.AddSeconds( 1 )
    );
  }
}