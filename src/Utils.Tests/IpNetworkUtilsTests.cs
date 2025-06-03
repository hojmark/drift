using System.Net;

namespace Drift.Utils.Tests;

public class IpNetworkUtilsTests {
  //TODO test all cases - there's not that many
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


  [TestCase( "0.0.0.0", 4294967296L )]
  [TestCase( "128.0.0.0", 2147483648L )]
  [TestCase( "255.0.0.0", 16777216L )]
  [TestCase( "255.255.0.0", 65536L )]
  [TestCase( "255.255.255.0", 256L )]
  [TestCase( "255.255.255.128", 128L )]
  [TestCase( "255.255.255.192", 64L )]
  [TestCase( "255.255.255.224", 32L )]
  [TestCase( "255.255.255.240", 16L )]
  [TestCase( "255.255.255.248", 8L )]
  [TestCase( "255.255.255.252", 4L )]
  [TestCase( "255.255.255.254", 2L )]
  [TestCase( "255.255.255.255", 1L )]
  public void IpRangeCountTest( string mask, long expectedCount ) {
    var count = IpNetworkUtils.GetIpRangeCount( IPAddress.Parse( mask ), usable: false );
    Assert.That( count, Is.EqualTo( expectedCount ) );
  }


  //TODO test other methods
}