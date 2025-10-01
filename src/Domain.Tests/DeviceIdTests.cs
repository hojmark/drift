using Drift.Domain.Device;
using Drift.Domain.Device.Addresses;
using Drift.Domain.Device.Declared;

namespace Drift.Domain.Tests;

internal sealed class DeviceIdTests {
  private static IEnumerable<TestCaseData> DeviceIds {
    get {
      yield return new TestCaseData(
        new DeviceId( [
          new IpV4Address( "192.168.0.100" )
        ] ),
        "IpV4=192.168.0.100"
      ).SetName( "Just IPv4" );
      yield return new TestCaseData(
        new DeviceId( [
          new MacAddress( "D4-E1-8C-98-0B-CB" )
        ] ),
        "Mac=D4-E1-8C-98-0B-CB"
      ).SetName( "Just MAC" );
      yield return new TestCaseData(
        new DeviceId( [
          new HostnameAddress( "host.domain.local" )
        ] ),
        "Hostname=host.domain.local"
      ).SetName( "Just hostname" );
      ;
      yield return new TestCaseData(
        new DeviceId( [
          new IpV4Address( "192.168.0.100" ),
          new MacAddress( "D4-E1-8C-98-0B-CB" )
        ] ),
        // TODO Consider if this is correct:
        //"IpV4=192.168.0.100"
        "IpV4=192.168.0.100|Mac=D4-E1-8C-98-0B-CB"
      ).SetName( "IPv4 and MAC - no IsId" );
      yield return new TestCaseData(
        new DeviceId( [
          new MacAddress( "D4-E1-8C-98-0B-CB" ),
          new IpV4Address( "192.168.0.100" )
        ] ),
        // TODO Consider if this is correct:
        //"IpV4=192.168.0.100"
        "IpV4=192.168.0.100|Mac=D4-E1-8C-98-0B-CB"
      ).SetName( "IPv4 and MAC - no IsId - reversed order" );
      yield return new TestCaseData(
        new DeviceId( [
          new IpV4Address( "192.168.0.100", true ),
          new MacAddress( "D4-E1-8C-98-0B-CB" )
        ] ),
        "IpV4=192.168.0.100"
      ).SetName( "IPv4 and MAC - IPv4 is ID" );
      yield return new TestCaseData(
        new DeviceId( [
          new MacAddress( "D4-E1-8C-98-0B-CB" ),
          new IpV4Address( "192.168.0.100", true )
        ] ),
        "IpV4=192.168.0.100"
      ).SetName( "IPv4 and MAC - IPv4 is ID - reversed order" );
      yield return new TestCaseData(
        new DeviceId( [
          new IpV4Address( "192.168.0.100" ),
          new MacAddress( "D4-E1-8C-98-0B-CB", true )
        ] ),
        "Mac=D4-E1-8C-98-0B-CB"
      ).SetName( "IPv4 and MAC - MAC is ID" );
      yield return new TestCaseData(
        new DeviceId( [
          new IpV4Address( "192.168.0.100", true ),
          new MacAddress( "D4-E1-8C-98-0B-CB", true )
        ] ),
        "IpV4=192.168.0.100|Mac=D4-E1-8C-98-0B-CB"
      ).SetName( "IPv4 and MAC - IPv4 and MAC is ID" );
      yield return new TestCaseData(
        new DeviceId( [
          new MacAddress( "D4-E1-8C-98-0B-CB", true ),
          new IpV4Address( "192.168.0.100", true )
        ] ),
        "IpV4=192.168.0.100|Mac=D4-E1-8C-98-0B-CB"
      ).SetName( "IPv4 and MAC - IPv4 and MAC is ID - reversed order" );
      yield return new TestCaseData(
        new DeviceId( [
          new IpV4Address( "192.168.0.100", true ),
          new HostnameAddress( "host.domain.local" ),
          new MacAddress( "D4-E1-8C-98-0B-CB", true )
        ] ),
        "IpV4=192.168.0.100|Mac=D4-E1-8C-98-0B-CB"
      ).SetName( "IPv4, hostname and MAC - IPv4 and MAC is ID" );
      yield return new TestCaseData(
        new DeviceId( [
          new MacAddress( "D4-E1-8C-98-0B-CB", true ),
          new HostnameAddress( "host.domain.local" ),
          new IpV4Address( "192.168.0.100", true )
        ] ),
        "IpV4=192.168.0.100|Mac=D4-E1-8C-98-0B-CB"
      ).SetName( "IPv4, hostname and MAC - IPv4 and MAC is ID - reversed order" );
    }
  }

  [TestCaseSource( nameof(DeviceIds) )]
  public void ContributionTest( DeviceId deviceId, string asString ) {
    Assert.That( deviceId.ToString(), Is.EqualTo( asString ) );
  }

  private static IEnumerable<TestCaseData> DeviceIdComparisons {
    get {
      yield return new TestCaseData(
        new DeclaredDevice { Addresses = [new IpV4Address( "192.168.123.1" ), new MacAddress( "D4-E1-8C-98-0B-CB" )] },
        new DeclaredDevice { Addresses = [new IpV4Address( "192.168.123.1" ), new MacAddress( "D4-E1-8C-98-0B-CB" )] },
        true,
        true
      ).SetName( "Same - exact match" );
      yield return new TestCaseData(
        new DeclaredDevice { Addresses = [new IpV4Address( "192.168.123.1" ), new MacAddress( "D4-E1-8C-98-0B-CB" )] },
        new DeclaredDevice { Addresses = [new IpV4Address( "192.168.123.1" )] },
        true,
        true
      ).SetName( "Same - partial match" );
      yield return new TestCaseData(
        new DeclaredDevice { Addresses = [] },
        new DeclaredDevice { Addresses = [] },
        true,
        false
      ).SetName( "Different - both zero" );
      yield return new TestCaseData(
        new DeclaredDevice { Addresses = [] },
        new DeclaredDevice { Addresses = [new IpV4Address( "192.168.123.2" )] },
        false,
        false
      ).SetName( "Different - first zero" );
      yield return new TestCaseData(
        new DeclaredDevice { Addresses = [new IpV4Address( "192.168.123.2" )] },
        new DeclaredDevice { Addresses = [] },
        true,
        false
      ).SetName( "Different - second zero" );
      yield return new TestCaseData(
        new DeclaredDevice { Addresses = [new IpV4Address( "192.168.123.1" ), new MacAddress( "D4-E1-8C-98-0B-CB" )] },
        new DeclaredDevice { Addresses = [new IpV4Address( "192.168.123.2" ), new MacAddress( "D4-E1-8C-98-0B-CB" )] },
        false,
        false
      ).SetName( "Different - partially same" );
      yield return new TestCaseData(
        new DeclaredDevice { Addresses = [new IpV4Address( "192.168.123.1" )] },
        new DeclaredDevice { Addresses = [new IpV4Address( "192.168.123.2" )] },
        false,
        false
      ).SetName( "Different - same type" );
      yield return new TestCaseData(
        new DeclaredDevice { Addresses = [new MacAddress( "D4-E1-8C-98-0B-CB" )] },
        new DeclaredDevice { Addresses = [new IpV4Address( "192.168.123.1" )] },
        false,
        false
      ).SetName( "Different - different type" );
      yield return new TestCaseData(
        new DeclaredDevice { Addresses = [new IpV4Address( "192.168.123.1" ), new MacAddress( "D4-E1-8C-98-0B-CB" )] },
        new DeclaredDevice { Addresses = [new IpV4Address( "192.168.123.1" )] },
        true,
        true
      ).SetName( "1" );
      yield return new TestCaseData(
        new DeclaredDevice { Addresses = [new IpV4Address( "192.168.123.1" ), new MacAddress( "D4-E1-8C-98-0B-CB" )] },
        new DeclaredDevice { Addresses = [new MacAddress( "D4-E1-8C-98-0B-CB" )] },
        true,
        true
      ).SetName( "2" );
      yield return new TestCaseData(
        new DeclaredDevice { Addresses = [new IpV4Address( "192.168.123.1" )] },
        new DeclaredDevice { Addresses = [new IpV4Address( "192.168.123.1" ), new MacAddress( "D4-E1-8C-98-0B-CB" )] },
        false,
        true
      ).SetName( "3" );
      yield return new TestCaseData(
        new DeclaredDevice { Addresses = [new MacAddress( "D4-E1-8C-98-0B-CB" )] },
        new DeclaredDevice { Addresses = [new IpV4Address( "192.168.123.1" ), new MacAddress( "D4-E1-8C-98-0B-CB" )] },
        false,
        true
      ).SetName( "4" );
    }
  }

  [TestCaseSource( nameof(DeviceIdComparisons) )]
  public void ComparisonTest( IAddressableDevice device1, IAddressableDevice device2, bool isContained, bool isSame ) {
    using ( Assert.EnterMultipleScope() ) {
      /*Assert.That(
        device1.GetDeviceId().Contains( device2.GetDeviceId() ), Is.EqualTo( isContained ),
        isContained
          ? "Expected to contain"
          : "Expected not to contain"
      );*/
      Assert.That(
        device1.GetDeviceId().Equals( device2.GetDeviceId() ), Is.EqualTo( isSame ),
        isSame
          ? "Expected to be same (using Equals)"
          : "Expected not to be same (using Equals)"
      );
      Assert.That(
        device1.GetDeviceId() == device2.GetDeviceId(), Is.EqualTo( isSame ),
        isSame
          ? "Expected to be same (using ==)"
          : "Expected not to be same (using ==)"
      );
      Assert.That(
        device2.GetDeviceId().Equals( device1.GetDeviceId() ), Is.EqualTo( isSame ),
        isSame
          ? "Expected to be same (using Equals) [commutative usage]"
          : "Expected not to be same (using Equals) [commutative usage]"
      );
      Assert.That(
        device2.GetDeviceId() == device1.GetDeviceId(), Is.EqualTo( isSame ),
        isSame
          ? "Expected to be same (using ==) [commutative usage]"
          : "Expected not to be same (using ==) [commutative usage]"
      );
    }
  }
}