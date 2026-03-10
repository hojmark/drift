using System.Net;
using Drift.Common.Network;
using Drift.Domain;

namespace Drift.Common.Tests;

internal sealed class IpNetworkUtilsTests {
  // TODO test all cases - there's not that many
  [TestCase( "0.0.0.0", 0 )]
  [TestCase( "128.0.0.0", 1 )]
  [TestCase( "255.0.0.0", 8 )]
  [TestCase( "255.255.0.0", 16 )]
  [TestCase( "255.255.255.0", 24 )]
  [TestCase( "255.255.255.128", 25 )]
  [TestCase( "255.255.255.192", 26 )]
  [TestCase( "255.255.255.224", 27 )]
  [TestCase( "255.255.255.240", 28 )]
  [TestCase( "255.255.255.248", 29 )]
  [TestCase( "255.255.255.252", 30 )]
  [TestCase( "255.255.255.254", 31 )]
  [TestCase( "255.255.255.255", 32 )]
  public void CidrPrefixLengthAndNetMaskConversionTest( string mask, int expectedPrefixLength ) {
    var prefixLength = IpNetworkUtils.GetCidrPrefixLength( IPAddress.Parse( mask ) );
    var subnet = IpNetworkUtils.GetNetmask( prefixLength );
    Assert.That( prefixLength, Is.EqualTo( expectedPrefixLength ) );
    Assert.That( subnet, Is.EqualTo( IPAddress.Parse( mask ) ) );
  }

  // TODO revise implementation, the three largest ranges take a long time
  // [TestCase( "0.0.0.0", 4294967296L, 4294967294L )]
  // [TestCase( "128.0.0.0", 2147483648L, 2147483646L )]
  // [TestCase( "255.0.0.0", 16777216L, 16777214L )]
  [TestCase( "255.255.0.0", 65536L, 65534L )]
  [TestCase( "255.255.255.0", 256L, 254L )]
  [TestCase( "255.255.255.128", 128L, 126L )]
  [TestCase( "255.255.255.192", 64L, 62L )]
  [TestCase( "255.255.255.224", 32L, 30L )]
  [TestCase( "255.255.255.240", 16L, 14L )]
  [TestCase( "255.255.255.248", 8L, 6L )]
  [TestCase( "255.255.255.252", 4L, 2L )]
  [TestCase( "255.255.255.254", 2L, 0L )]
  [TestCase( "255.255.255.255", 1L, 0L )]
  public void IpRangeCountTest( string mask, long expectedAllCount, long expectedUsableCount ) {
    // Arrange
    var cidr = new CidrBlock( "0.0.0.0/" + IpNetworkUtils.GetCidrPrefixLength( IPAddress.Parse( mask ) ) );

    // Act
    var countAll = IpNetworkUtils.GetIpRangeCount( cidr, usable: false );
    var countUsable = IpNetworkUtils.GetIpRangeCount( cidr, usable: true );

    // Assert
    using ( Assert.EnterMultipleScope() ) {
      Assert.That( countAll, Is.EqualTo( expectedAllCount ) );
      Assert.That( countUsable, Is.EqualTo( expectedUsableCount ) );
    }
  }

  [TestCase( "10.0.0.0", true )] // 10.0.0.0/8 — start
  [TestCase( "10.255.255.255", true )] // 10.0.0.0/8 — end
  [TestCase( "10.128.5.17", true )] // 10.0.0.0/8 — mid
  [TestCase( "172.16.0.0", true )] // 172.16.0.0/12 — start
  [TestCase( "172.31.255.255", true )] // 172.16.0.0/12 — end
  [TestCase( "172.20.100.1", true )] // 172.16.0.0/12 — mid
  [TestCase( "192.168.0.0", true )] // 192.168.0.0/16 — start
  [TestCase( "192.168.255.255", true )] // 192.168.0.0/16 — end
  [TestCase( "192.168.1.100", true )] // 192.168.0.0/16 — typical LAN
  [TestCase( "9.255.255.255", false )] // just below 10.0.0.0/8
  [TestCase( "11.0.0.0", false )] // just above 10.0.0.0/8
  [TestCase( "172.15.255.255", false )] // just below 172.16.0.0/12
  [TestCase( "172.32.0.0", false )] // just above 172.16.0.0/12
  [TestCase( "192.167.255.255", false )] // just below 192.168.0.0/16
  [TestCase( "192.169.0.0", false )] // just above 192.168.0.0/16
  [TestCase( "8.8.8.8", false )] // public IP
  [TestCase( "0.0.0.0", false )] // unspecified
  public void IsPrivateIpV4Test( string ip, bool expectedResult ) {
    Assert.That( IpNetworkUtils.IsPrivateIpV4( IPAddress.Parse( ip ) ), Is.EqualTo( expectedResult ) );
  }

  [Test]
  public void IsPrivateIpV4_ThrowsForIPv6() {
    Assert.Throws<ArgumentException>( () => IpNetworkUtils.IsPrivateIpV4( IPAddress.IPv6Loopback ) );
  }
}