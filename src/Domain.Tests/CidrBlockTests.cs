namespace Drift.Domain.Tests;

public class CidrBlockTests {
  [Test]
  [TestCase( "192.168.123.0/24" )]
  [TestCase( "2001:db8::/64" )]
  [TestCase( "192.168.123.0/24 " )]
  [TestCase( " 192.168.123.0/24" )]
  public void ValidCidrDoesNotThrowTest( string cidr ) {
    Assert.DoesNotThrow( () => _ = new CidrBlock( cidr ) );
    Assert.That( new CidrBlock( cidr ).ToString(), Is.EqualTo( cidr.Trim() ) );
  }

  [Test]
  [TestCase( "192 .168.123.0/24" )]
  [TestCase( "192.168.123.0" )]
  [TestCase( "192.168.123.0/-24" )]
  //TODO ipv6
  public void InvalidCidrThrowsTest( string cidr ) {
    //TODO more specific exception
    Assert.Catch( () => _ = new CidrBlock( cidr ) );
  }
}