using Drift.Cli.Tests.Utils.Network.Firewall;
using Drift.Domain;
using Drift.Domain.Device.Addresses;

namespace Drift.Cli.Tests.Utils;

/// <summary>
/// Tests for FirewallEvaluator functionality including rule matching,
/// priority evaluation, and device-level filtering.
/// </summary>
internal sealed class FirewallRulesTests {
  // Standard test subnets
  private static readonly List<(string Name, CidrBlock Cidr)> TestSubnets = [
    ( "guest", new CidrBlock( "192.168.100.0/24" ) ),
    ( "internal", new CidrBlock( "10.0.0.0/24" ) ),
    ( "external", new CidrBlock( "172.16.0.0/24" ) ),
    ( "dmz", new CidrBlock( "192.168.1.0/24" ) ),
    ( "internet", new CidrBlock( "1.0.0.0/8" ) ),
    ( "anywhere", new CidrBlock( "192.168.2.0/24" ) ),
    ( "source", new CidrBlock( "192.168.3.0/24" ) ),
    ( "dest", new CidrBlock( "192.168.4.0/24" ) ),
    ( "subnet1", new CidrBlock( "192.168.5.0/24" ) ),
    ( "subnet2", new CidrBlock( "192.168.6.0/24" ) ),
    ( "subnet3", new CidrBlock( "192.168.7.0/24" ) ),
    ( "subnet4", new CidrBlock( "192.168.8.0/24" ) )
  ];

  [Test]
  public void NoRules_DefaultAllow_ReturnsTrue() {
    // Arrange
    var rules = new FirewallRules();
    var evaluator = new FirewallEvaluator( rules, TestSubnets );

    // Act
    var allowed = evaluator.IsAllowed(
      FirewallTarget.Subnet( "guest" ),
      FirewallTarget.Subnet( "internal" )
    );

    // Assert
    Assert.That( allowed, Is.True );
  }

  [Test]
  public void SubnetNameMatching_ExactMatch_Works() {
    // Arrange
    var rules = new FirewallRules();
    rules.Deny( FirewallTarget.Subnet( "guest" ), FirewallTarget.Subnet( "internal" ) );
    var evaluator = new FirewallEvaluator( rules, TestSubnets );

    // Act
    var toExternalAllowed = evaluator.IsAllowed(
      FirewallTarget.Subnet( "guest" ),
      FirewallTarget.Subnet( "external" )
    );
    var toInternalAllowed = evaluator.IsAllowed(
      FirewallTarget.Subnet( "guest" ),
      FirewallTarget.Subnet( "internal" )
    );

    // Assert
    Assert.That( toExternalAllowed, Is.True );
    Assert.That( toInternalAllowed, Is.False );
  }

  [Test]
  public void FirstMatchWins_OrderMatters() {
    // Arrange
    var rules = new FirewallRules();
    rules.Deny( FirewallTarget.Subnet( "guest" ), FirewallTarget.Any );
    rules.Allow( FirewallTarget.Subnet( "guest" ), FirewallTarget.Subnet( "dmz" ) );
    var evaluator = new FirewallEvaluator( rules, TestSubnets );

    // Act
    var allowed = evaluator.IsAllowed(
      FirewallTarget.Subnet( "guest" ),
      FirewallTarget.Subnet( "dmz" )
    );

    // Assert
    Assert.That( allowed, Is.False );
  }

  [Test]
  public void FirstMatchWins_AllowBeforeDeny() {
    // Arrange
    var rules = new FirewallRules();
    rules.Allow( FirewallTarget.Subnet( "guest" ), FirewallTarget.Subnet( "dmz" ) );
    rules.Deny( FirewallTarget.Subnet( "guest" ), FirewallTarget.Any );
    var evaluator = new FirewallEvaluator( rules, TestSubnets );

    // Act
    var allowed = evaluator.IsAllowed(
      FirewallTarget.Subnet( "guest" ),
      FirewallTarget.Subnet( "dmz" )
    );

    // Assert
    Assert.That( allowed, Is.True );
  }

  [Test]
  public void DeviceIpMatching_ExactIp_Works() {
    // Arrange
    var rules = new FirewallRules();
    rules.Deny( FirewallTarget.Any, FirewallTarget.FromIp( new IpV4Address( "10.0.0.100" ) ) );
    var evaluator = new FirewallEvaluator( rules, TestSubnets );

    // Act
    var toDeniedAllowed = evaluator.IsAllowed(
      FirewallTarget.Subnet( "external" ),
      FirewallTarget.FromIp( new IpV4Address( "10.0.0.100" ) )
    );

    var toAllowedAllowed = evaluator.IsAllowed(
      FirewallTarget.Subnet( "external" ),
      FirewallTarget.FromIp( new IpV4Address( "10.0.0.101" ) )
    );

    // Assert
    Assert.That( toDeniedAllowed, Is.False );
    Assert.That( toAllowedAllowed, Is.True, "Traffic to other device should be allowed" );
  }

  [Test]
  public void DeviceIpMatching_SourceIp_Works() {
    // Arrange
    var rules = new FirewallRules();
    rules.Allow( FirewallTarget.FromIp( new IpV4Address( "192.168.1.10" ) ), FirewallTarget.Any );
    rules.Deny( FirewallTarget.Subnet( "dmz" ), FirewallTarget.Any );
    var evaluator = new FirewallEvaluator( rules, TestSubnets );

    // Act
    var bastionAllowed = evaluator.IsAllowed(
      FirewallTarget.FromIp( new IpV4Address( "192.168.1.10" ) ),
      FirewallTarget.Subnet( "internal" )
    );

    var otherDenied = evaluator.IsAllowed(
      FirewallTarget.FromIp( new IpV4Address( "192.168.1.20" ) ),
      FirewallTarget.Subnet( "internal" )
    );

    // Assert
    Assert.That( bastionAllowed, Is.True, "Bastion host should be allowed" );
    Assert.That( otherDenied, Is.False, "Other DMZ hosts should be denied" );
  }

  [Test]
  public void CidrMatching_DeviceInRange_Matches() {
    // Arrange
    var rules = new FirewallRules();
    rules.Deny( FirewallTarget.FromCidr( new CidrBlock( "192.168.1.0/24" ) ), FirewallTarget.Any );
    var evaluator = new FirewallEvaluator( rules, TestSubnets );

    // Act
    var deviceInRange = evaluator.IsAllowed(
      FirewallTarget.FromIp( new IpV4Address( "192.168.1.50" ) ),
      FirewallTarget.Subnet( "anywhere" )
    );

    var deviceOutOfRange = evaluator.IsAllowed(
      FirewallTarget.FromIp( new IpV4Address( "192.168.2.50" ) ),
      FirewallTarget.Subnet( "anywhere" )
    );

    // Assert
    Assert.That( deviceInRange, Is.False, "Device in CIDR range should be blocked" );
    Assert.That( deviceOutOfRange, Is.True, "Device outside CIDR range should be allowed" );
  }

  [Test]
  public void CidrMatching_DestinationCidr_Works() {
    // Arrange
    var rules = new FirewallRules();
    rules.Allow( FirewallTarget.Subnet( "external" ), FirewallTarget.FromCidr( new CidrBlock( "10.0.0.0/8" ) ) );
    rules.Deny( FirewallTarget.Subnet( "external" ), FirewallTarget.Any );
    var evaluator = new FirewallEvaluator( rules, TestSubnets );

    // Act
    var allowedRange = evaluator.IsAllowed(
      FirewallTarget.Subnet( "external" ),
      FirewallTarget.FromIp( new IpV4Address( "10.5.10.100" ) )
    );

    var deniedRange = evaluator.IsAllowed(
      FirewallTarget.Subnet( "external" ),
      FirewallTarget.FromIp( new IpV4Address( "192.168.1.100" ) )
    );

    // Assert
    Assert.That( allowedRange, Is.True, "Device in allowed CIDR range should pass" );
    Assert.That( deniedRange, Is.False, "Device outside allowed range should be denied" );
  }

  [Test]
  public void MixedRules_SubnetAndDeviceLevel_Work() {
    // Arrange
    var rules = new FirewallRules();
    rules.Allow(
      FirewallTarget.FromIp( new IpV4Address( "192.168.1.10" ) ),
      FirewallTarget.FromIp( new IpV4Address( "10.0.0.100" ) )
    );
    rules.Deny( FirewallTarget.Subnet( "dmz" ), FirewallTarget.Subnet( "internal" ) );
    var evaluator = new FirewallEvaluator( rules, TestSubnets );

    // Act
    var specificAllowed = evaluator.IsAllowed(
      FirewallTarget.FromIp( new IpV4Address( "192.168.1.10" ) ),
      FirewallTarget.FromIp( new IpV4Address( "10.0.0.100" ) )
    );

    var genericDenied = evaluator.IsAllowed(
      FirewallTarget.Subnet( "dmz" ),
      FirewallTarget.Subnet( "internal" )
    );

    // Assert
    Assert.That( specificAllowed, Is.True, "Specific device exception should be allowed" );
    Assert.That( genericDenied, Is.False, "Generic subnet traffic should be denied" );
  }

  [Test]
  public void GuestNetwork_Isolation_DevicesCantSeeEachOther() {
    // Arrange
    var rules = new FirewallRules();
    rules.Deny( FirewallTarget.Subnet( "guest" ), FirewallTarget.Subnet( "guest" ) );
    rules.Allow( FirewallTarget.Subnet( "guest" ), FirewallTarget.Subnet( "internet" ) );
    var evaluator = new FirewallEvaluator( rules, TestSubnets );

    // Act
    var guestToGuest = evaluator.IsAllowed(
      FirewallTarget.Subnet( "guest" ),
      FirewallTarget.Subnet( "guest" )
    );
    var guestToInternet = evaluator.IsAllowed(
      FirewallTarget.Subnet( "guest" ),
      FirewallTarget.Subnet( "internet" )
    );

    // Assert
    Assert.That( guestToGuest, Is.False );
    Assert.That( guestToInternet, Is.True );
  }

  [Test]
  public void AnyKeyword_WorksSameAsWildcard() {
    // Arrange
    var rules = new FirewallRules();
    rules.Deny( FirewallTarget.Any, FirewallTarget.Subnet( "internal" ) );
    var evaluator = new FirewallEvaluator( rules, TestSubnets );

    // Act
    var allowed = evaluator.IsAllowed(
      FirewallTarget.Subnet( "external" ),
      FirewallTarget.Subnet( "internal" )
    );

    // Assert
    Assert.That( allowed, Is.False );
  }

  [Test]
  public void InvalidCidr_DoesNotCrash_ReturnsFalse() {
    // Arrange
    var rules = new FirewallRules();
    rules.Allow( FirewallTarget.Subnet( "invalid-cidr" ), FirewallTarget.Any );
    var evaluator = new FirewallEvaluator( rules, TestSubnets );

    // Act / Assert
    Assert.DoesNotThrow( () => {
      var result = evaluator.IsAllowed(
        FirewallTarget.Subnet( "source" ),
        FirewallTarget.Subnet( "dest" )
      );
      Assert.That( result, Is.True, "Subnet name 'invalid-cidr' should not match 'source', fall through to default" );
    } );
  }
}