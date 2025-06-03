using Drift.Cli.Commands.Scan;
using Drift.Domain;
using Drift.Domain.Scan;
using Drift.TestUtilities.NetworkProviders;

namespace Drift.Cli.Tests;

public class NetworkScannerTests {
  [Explicit( "need to mock network provider" )] //TODO fix
  [Test]
  [TestCase( "192.168.0.1/24" )]
  //[TestCase( "192.168.123.0/24" )]
  //[TestCase( "2001:db8::/64" )]
  public async Task BasicTest( string cidr ) {
    // Arrange
    var networkProvider = new TestNetworkProvider { };
    var subnet = new CidrBlock( cidr );

    // Act
    var result = await new PingNetworkScanner( new NullOutputManager() ).ScanAsync( subnet /*, networkProvider*/ );

    // Assert
    Assert.That( result, Is.Not.Null );
    Assert.That( result.Status, Is.EqualTo( ScanResultStatus.Success ) );
    await Verify( result.DiscoveredDevices );
  }
}