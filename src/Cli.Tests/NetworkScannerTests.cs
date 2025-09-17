using System.Net;
using System.Text.RegularExpressions;
using Drift.Cli.Output;
using Drift.Cli.Scan;
using Drift.Cli.Tests.Utils;
using Drift.Domain;
using Drift.Domain.Scan;

namespace Drift.Cli.Tests;

public class NetworkScannerTests {
  [Test]
  [TestCase( "192.168.0.0/24" )]
  [TestCase( "192.168.0.1/24" )] // TODO should fail
  [TestCase( "192.168.123.0/24" )]
  //[TestCase( "2001:db8::/64" )]
  [TestCase( "10.255.255.254/32", "192.168.32.0/20", "172.19.0.0/16" )]
  public async Task BasicTest( params string[] cidrs ) {
    // Arrange
    var subnets = cidrs.Select( cidr => new CidrBlock( cidr ) ).ToList();
    var successfulIps = subnets.SelectMany( cidr => IPNetwork2
      .Parse( cidr.ToString() )
      .ListIPAddress( FilterEnum.Usable )
      .Select( ip => IPAddress.Parse( ip.ToString() ) )
      .Take( 3 )
    ).ToList();
    var pingTool = new TestPingTool( successfulIps );

    var stdOut = new StringWriter();
    var errOut = new StringWriter();
    var testOutputManager =
      new OutputManagerFactory( false ).Create( OutputFormat.Normal, true, stdOut, errOut, true );

    var scanner = new PingNetworkScanner( testOutputManager, pingTool );

    // Act
    var result = await scanner.ScanAsync(
      new ScanRequest { Cidrs = subnets, MaxPingsPerSecond = int.MaxValue } /*, networkProvider*/
    );

    // Assert
    Assert.That( result, Is.Not.Null );
    Assert.That( result.Status, Is.EqualTo( ScanResultStatus.Success ) );
    await Verify( stdOut.ToString() + errOut )
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