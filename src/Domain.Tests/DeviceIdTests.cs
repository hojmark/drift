using Drift.Domain.Device;
using Drift.Domain.Device.Addresses;
using Drift.Domain.Device.Declared;

namespace Drift.Domain.Tests;

public class DeviceIdTests {
  private static IEnumerable<TestCaseData> DeviceIds {
    get {
      yield return new TestCaseData(
        new DeclaredDevice { Addresses = [new IpV4Address( "192.168.123.1" ), new MacAddress( "d4:e1:8c:98:0b:cb" )] },
        new DeclaredDevice { Addresses = [new IpV4Address( "192.168.123.1" ), new MacAddress( "d4:e1:8c:98:0b:cb" )] },
        true,
        true
      ).SetName( "Same - exact match" );
      yield return new TestCaseData(
        new DeclaredDevice { Addresses = [new IpV4Address( "192.168.123.1" ), new MacAddress( "d4:e1:8c:98:0b:cb" )] },
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
        new DeclaredDevice { Addresses = [new IpV4Address( "192.168.123.1" ), new MacAddress( "d4:e1:8c:98:0b:cb" )] },
        new DeclaredDevice { Addresses = [new IpV4Address( "192.168.123.2" ), new MacAddress( "d4:e1:8c:98:0b:cb" )] },
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
        new DeclaredDevice { Addresses = [new MacAddress( "d4:e1:8c:98:0b:cb" )] },
        new DeclaredDevice { Addresses = [new IpV4Address( "192.168.123.1" )] },
        false,
        false
      ).SetName( "Different - different type" );
      yield return new TestCaseData(
        new DeclaredDevice { Addresses = [new IpV4Address( "192.168.123.1" ), new MacAddress( "d4:e1:8c:98:0b:cb" )] },
        new DeclaredDevice { Addresses = [new IpV4Address( "192.168.123.1" )] },
        true,
        true
      ).SetName( "1" );
      yield return new TestCaseData(
        new DeclaredDevice { Addresses = [new IpV4Address( "192.168.123.1" ), new MacAddress( "d4:e1:8c:98:0b:cb" )] },
        new DeclaredDevice { Addresses = [new MacAddress( "d4:e1:8c:98:0b:cb" )] },
        true,
        true
      ).SetName( "2" );
      yield return new TestCaseData(
        new DeclaredDevice { Addresses = [new IpV4Address( "192.168.123.1" )] },
        new DeclaredDevice { Addresses = [new IpV4Address( "192.168.123.1" ), new MacAddress( "d4:e1:8c:98:0b:cb" )] },
        false,
        true
      ).SetName( "3" );
      yield return new TestCaseData(
        new DeclaredDevice { Addresses = [new MacAddress( "d4:e1:8c:98:0b:cb" )] },
        new DeclaredDevice { Addresses = [new IpV4Address( "192.168.123.1" ), new MacAddress( "d4:e1:8c:98:0b:cb" )] },
        false,
        true
      ).SetName( "4" );
    }
  }

  [TestCaseSource( nameof(DeviceIds) )]
  public void ContainsTest( IAddressableDevice device1, IAddressableDevice device2, bool isContained, bool isSame ) {
    using ( Assert.EnterMultipleScope() ) {
      Assert.That(
        device1.GetDeviceId().Contains( device2.GetDeviceId() ), Is.EqualTo( isContained ),
        isContained ? "Expected to contain" : "Expected not to contain"
      );
      Assert.That(
        device1.GetDeviceId().IsSame( device2.GetDeviceId() ), Is.EqualTo( isSame ),
        isSame ? "Expected to be same" : "Expected not to be same"
      );
    }
  }
}