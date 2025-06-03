using Drift.Diff.Domain;
using Drift.Domain;
using Drift.Domain.Device.Addresses;
using Drift.Domain.Device.Declared;
using Drift.Domain.Device.Discovered;
using Drift.Domain.Extensions;
using Drift.Domain.Scan;
using Drift.Parsers.EnvironmentJson;
using Drift.TestUtilities;

namespace Drift.Diff.Tests;

public class DiffTest {
  private static readonly ScanResult ScanResult1 = new() {
    Metadata = new Metadata {
      StartedAt = DateTime.Parse( "2025-04-24T12:20:08.4219405+02:00" ).ToUniversalTime(),
      EndedAt = DateTime.Parse( "2023-01-01" )
    },
    Status = ScanResultStatus.Success,
    DiscoveredDevices = [
      new DiscoveredDevice { Addresses = [new IpV4Address( "192.168.0.10" )] },
      new DiscoveredDevice {
        Addresses = [new IpV4Address( "192.168.0.21" ), new MacAddress( "ABC" )], Ports = [443, 80]
      },
      new DiscoveredDevice {
        Addresses = [new IpV4Address( "192.168.0.22" ), new MacAddress( "abcdefghijklmnopqrstu" )]
      }
    ]
  };

  private static readonly ScanResult ScanResult2 = new() {
    Metadata = new Metadata {
      StartedAt = DateTime.Parse( "2025-04-24T12:20:08.4219405+02:00" ).ToUniversalTime(),
      EndedAt = DateTime.Parse( "2023-01-01" )
    },
    Status = ScanResultStatus.Success,
    DiscoveredDevices = [
      new DiscoveredDevice { Addresses = [new IpV4Address( "192.168.0.10" )] },
      new DiscoveredDevice {
        Addresses = [new IpV4Address( "192.168.0.21" ), new MacAddress( "DEF" )], Ports = [22, 443, 80]
      },
      new DiscoveredDevice { Addresses = [new IpV4Address( "192.168.0.150" )] }
    ]
  };

  [Test]
  public void SnapshotIndexAsKeyTest() {
    var diffs = ObjectDiffEngine.Compare( ScanResult1, ScanResult2, nameof(ScanResult) );

    Print( diffs );
    var diffsAsJson = JsonConverter.Serialize( diffs );
    Verify( diffsAsJson );
  }

  [Test]
  public Task SnapshotIpAsKeySelectorTest() {
    // Arrange
    var options = new DiffOptions()
      .SetDiffTypesAll()
      .SetKeySelector<DiscoveredDevice>( device => device.Get( AddressType.IpV4 ) )
      .SetKeySelector<Port>( port => port.Value.ToString() );

    // Act
    var diffs = ObjectDiffEngine.Compare( ScanResult1, ScanResult2, nameof(ScanResult), options );

    // Assert
    Print( diffs );
    // TODO custom serializer that skips properties not directly defined on the object (e.g. lists, objects)
    var diffsAsJson = JsonConverter.Serialize( diffs );
    return Verify( diffsAsJson );
  }

  [Test]
  public Task SnapshotDefaultKeySelectorTest() {
    // Arrange
    var testLogger = new TestLogger();
    var options = new DiffOptions()
      .ConfigureDiffDeviceKeySelectors();

    // Act
    var diffs = ObjectDiffEngine.Compare(
      ScanResult1.DiscoveredDevices.ToDiffDevices(),
      ScanResult2.DiscoveredDevices.ToDiffDevices(),
      nameof(ScanResult),
      options,
      testLogger
    );

    // Assert
    //Print( diffs );
    var diffsAsJson = JsonConverter.Serialize( diffs );
    return Verify( diffsAsJson );
  }


  [Test]
  public Task UnchangedUsingNoKeySelectorTest() {
    // Arrange
    var options = new DiffOptions()
      .SetDiffTypesAll();

    var original = new List<DiscoveredDevice> {
      new() { Addresses = [new IpV4Address( "192.168.0.10" )] },
      // new() { Addresses = [new IPv4Address( "192.168.0.21" ), new MacAddress( "ABC" )], Ports = [443, 80] },
      // new() { Addresses = [new IPv4Address( "192.168.0.22" ), new MacAddress( "abcdefghijklmnopqrstu" )] }
    }.ToDiffDevices();

    var updated = new List<DiscoveredDevice> {
      new() { Addresses = [new IpV4Address( "192.168.0.10" )] },
      //  new() { Addresses = [new IPv4Address( "192.168.0.21" ), new MacAddress( "DEF" )], Ports = [22, 443, 80] },
      // new() { Addresses = [new IPv4Address( "192.168.0.150" )] }
    }.ToDiffDevices();

    // Act
    var diffs = ObjectDiffEngine.Compare( original, updated, nameof(ScanResult), options );

    // Assert
    Print( diffs );
    var diffsAsJson = JsonConverter.Serialize( diffs );
    return Verify( diffsAsJson );
  }

  [Test]
  public Task UnchangedUsingKeySelectorTest() {
    // Arrange
    var options = new DiffOptions()
      .ConfigureDiffDeviceKeySelectors()
      .SetDiffTypesAll();

    var original = new List<DiscoveredDevice> {
      new() { Addresses = [new IpV4Address( "192.168.0.10" )] },
      // new() { Addresses = [new IPv4Address( "192.168.0.21" ), new MacAddress( "ABC" )], Ports = [443, 80] },
      // new() { Addresses = [new IPv4Address( "192.168.0.22" ), new MacAddress( "abcdefghijklmnopqrstu" )] }
    }.ToDiffDevices();

    var updated = new List<DiscoveredDevice> {
      new() { Addresses = [new IpV4Address( "192.168.0.10" )] },
      //  new() { Addresses = [new IPv4Address( "192.168.0.21" ), new MacAddress( "DEF" )], Ports = [22, 443, 80] },
      // new() { Addresses = [new IPv4Address( "192.168.0.150" )] }
    }.ToDiffDevices();

    // Act
    var diffs = ObjectDiffEngine.Compare( original, updated, nameof(ScanResult), options );

    // Assert
    Print( diffs );
    var diffsAsJson = JsonConverter.Serialize( diffs );
    return Verify( diffsAsJson );
  }

  [Test]
  public Task SubListAddedTest() {
    // Arrange
    var options = new DiffOptions()
      .ConfigureDiffDeviceKeySelectors()
      .SetDiffTypesAll();

    var original = new List<DiscoveredDevice> {
      //new() { Addresses = [new IPv4Address( "192.168.0.10" )] },
      new() { Addresses = [new IpV4Address( "192.168.0.21" )], Ports = [443, 80] },
      // new() { Addresses = [new IPv4Address( "192.168.0.22" ), new MacAddress( "abcdefghijklmnopqrstu" )] }
    }.ToDiffDevices();

    var updated = new List<DiscoveredDevice> {
      //new() { Addresses = [new IPv4Address( "192.168.0.10" )] },
      new() { Addresses = [new IpV4Address( "192.168.0.21" )], Ports = [22, 443, 80] },
      // new() { Addresses = [new IPv4Address( "192.168.0.150" )] }
    }.ToDiffDevices();

    // Act
    var diffs = ObjectDiffEngine.Compare( original, updated, nameof(ScanResult), options );

    // Assert
    Print( diffs );
    var diffsAsJson = JsonConverter.Serialize( diffs );
    return Verify( diffsAsJson );
  }

  [Test]
  public Task SubListRemovedTest() {
    // Arrange
    var options = new DiffOptions()
      .ConfigureDiffDeviceKeySelectors()
      .SetDiffTypesAll();

    var original = new List<DiscoveredDevice> {
      new() { Addresses = [new IpV4Address( "192.168.0.21" )], Ports = [22, 443, 80] }
    }.ToDiffDevices();

    var updated = new List<DiscoveredDevice> {
      new() { Addresses = [new IpV4Address( "192.168.0.21" )], Ports = [443, 80] }
    }.ToDiffDevices();

    // Act
    var diffs = ObjectDiffEngine.Compare( original, updated, nameof(ScanResult), options );

    // Assert
    Print( diffs );
    var diffsAsJson = JsonConverter.Serialize( diffs );
    return Verify( diffsAsJson );
  }


  [Test]
  public void MatchDeclaredAndDiscoveredTest() {
    List<DeclaredDevice> declaredDevices = [
      new() { Addresses = [new HostnameAddress( "t14", IsId: true ), new MacAddress( "t14-MAC", IsId: false )] },
    ];
    List<DiscoveredDevice> discoveredDevices = [
      new() { Addresses = [new HostnameAddress( "t14" )] }
    ];

    var original = declaredDevices.ToDiffDevices();
    var updated = discoveredDevices.ToDiffDevices();

    var diffs = ObjectDiffEngine.Compare( original, updated, "Device",
      new DiffOptions { IgnorePaths = ["Device[*].Addresses[*].Required"] }
        .ConfigureDiffDeviceKeySelectors()
        .SetDiffTypesAll()
    );

    Print( diffs );
    var diffsAsJson = JsonConverter.Serialize( diffs );
    Verify( diffsAsJson );
  }

  /*[Explicit]
  [Test]
  public void DiffDemo0Test() {
    var demo0Spec = SharedTestResourceProvider.GetStream( "SPEC_YAML" );
    var network = YamlConverter.Deserialize( demo0Spec );

    var demo0NmapXml = SharedTestResourceProvider.GetStream( "NMAP_XML" );
    var nmaprun = NmapXmlReader.Deserialize( demo0NmapXml );

    var declaredDevices = network.Devices.Where( d => d.Enabled ?? true );
    var discoveredDevices = NmapConverter.ToDevices( nmaprun );

    var original = declaredDevices.ToDiffDevices();
    var updated = discoveredDevices.ToDiffDevices();

    var diffs = ObjectDiffEngine.Compare( original, updated, nameof(ScanResult),
      new DiffOptions { IgnorePaths = ["ScanResult[*].Addresses[*].Required", "ScanResult[*].Ports[*]"] }
        .ConfigureDiffDeviceKeySelectors()
      // .SetDiffTypesAll()
    );

    Print( diffs );
    var diffsAsJson = JsonConverter.Serialize( diffs );
    Verify( diffsAsJson );
  }*/

  private static void Print( List<ObjectDiff> diffs ) {
    foreach ( var diff in diffs ) {
      Console.WriteLine( $"{diff.PropertyPath}: {diff.DiffType} — '{diff.Original}' → '{diff.Updated}'" );
    }
  }
}