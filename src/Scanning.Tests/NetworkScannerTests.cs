using System.Net;
using System.Text.RegularExpressions;
using Drift.Domain;
using Drift.Domain.Scan;
using Drift.Scanning.Scanners;
using Drift.Scanning.Tests.Utils;
using Drift.TestUtilities;

namespace Drift.Scanning.Tests;

internal sealed class NetworkScannerTests {
  [Test]
  [TestCase( "192.168.0.0/24" )]
  [TestCase( "192.168.0.1/24" )] // TODO should fail
  [TestCase( "192.168.123.0/24" )]
  //[TestCase( "2001:db8::/64" )]
  [TestCase( "10.255.255.254/32", "192.168.32.0/20" /* TODO too slow to run in unit test: , "172.19.0.0/16" */ )]
  public async Task BasicTest( params string[] cidrs ) {
    // Arrange
    var subnets = cidrs.Select( cidr => new CidrBlock( cidr ) ).ToList();
    var successfulIps = subnets.SelectMany( cidr => IPNetwork2
      .Parse( cidr.ToString() )
      .ListIPAddress( Filter.Usable )
      .Select( ip => IPAddress.Parse( ip.ToString() ) )
      .Take( 3 )
    ).ToList();

    var subnetScanner = new LinuxPingSubnetScanner( new TestPingTool( successfulIps ) );

    var logger = new StringLogger();

    var networkScanner = new DefaultNetworkScanner( new PredefinedSubnetScannerFactory( subnetScanner ) );

    // Act
    var result = await networkScanner.ScanAsync(
      new NetworkScanOptions { Cidrs = subnets, PingsPerSecond = int.MaxValue } /*, networkProvider*/, logger
    );

    // Assert
    Assert.That( result, Is.Not.Null );
    Assert.That( result.Status, Is.EqualTo( ScanResultStatus.Success ) );
    await Verify( logger.ToString() )
      .ScrubLinesWithReplace( line =>
        Regex.Replace(
          Regex.Replace(
            line,
            @"\d{2}/\d{2}/\d{4} \d{2}:\d{2}:\d{2}",
            "<time>"
          ),
          "in .+",
          "in <elapsed>"
        )
      );
  }
}