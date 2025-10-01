using System.Text.Json;
using System.Text.Json.Serialization;
using Drift.Diff.Domain;
using Drift.Domain;
using Drift.Domain.Device.Addresses;
using Drift.Domain.Device.Declared;
using Drift.Domain.Device.Discovered;
using Drift.Domain.Extensions;
using Drift.Domain.Scan;
using Drift.TestUtilities;
using JsonConverter = Drift.EnvironmentConfig.JsonConverter;

namespace Drift.Diff.Tests;

internal sealed class DiffTest {
  private static readonly NetworkScanResult ScanResult1 = new() {
    Metadata = new Metadata {
      StartedAt = DateTime.Parse( "2025-04-24T12:20:08.4219405+02:00" ).ToUniversalTime(),
      EndedAt = DateTime.Parse( "2023-01-01" )
    },
    Status = ScanResultStatus.Success,
    Subnets = [
      new SubnetScanResult {
        CidrBlock = new CidrBlock( "192.168.0.0/24" ),
        Metadata = null,
        Status = ScanResultStatus.Success,
        DiscoveredDevices = [
          new DiscoveredDevice { Addresses = [new IpV4Address( "192.168.0.10" )] },
          /*new DiscoveredDevice {
            Addresses = [new IpV4Address( "192.168.0.21" ), new MacAddress( "ABC" )] //, Ports = [443, 80]
          },*/
          new DiscoveredDevice {
            Addresses = [new IpV4Address( "192.168.0.22" ), new MacAddress( "22-22-22-22-22-22" )]
          }
        ]
      }
    ]
  };

  private static readonly NetworkScanResult ScanResult2 = new() {
    Metadata = new Metadata {
      StartedAt = DateTime.Parse( "2025-04-24T12:20:08.4219405+02:00" ).ToUniversalTime(),
      EndedAt = DateTime.Parse( "2023-01-01" )
    },
    Status = ScanResultStatus.Success,
    Subnets = [
      new SubnetScanResult {
        CidrBlock = new CidrBlock( "192.168.0.0/24" ),
        Metadata = null,
        Status = ScanResultStatus.Success,
        DiscoveredDevices = [
          new DiscoveredDevice { Addresses = [new IpV4Address( "192.168.0.10" )] },
          /*new DiscoveredDevice {
            Addresses = [new IpV4Address( "192.168.0.21" ), new MacAddress( "DEF" )] //, Ports = [22, 443, 80]
          },*/
          new DiscoveredDevice { Addresses = [new IpV4Address( "192.168.0.150" )] }
        ]
      }
    ]
  };

  [Test]
  public void IndexAsKeyTest() {
    var diffs = ObjectDiffEngine.Compare( ScanResult1, ScanResult2, nameof(NetworkScanResult) );

    Print( diffs );
    var diffsAsJson = JsonConverter.Serialize( diffs );
    Verify( diffsAsJson );
  }

  [Test]
  public Task DefaultKeySelectorTest() {
    // Arrange
    var testLogger = new StringLogger();
    var options = new DiffOptions()
      .ConfigureDiffDeviceKeySelectors( [] );

    // Act
    var diffs = ObjectDiffEngine.Compare(
      ScanResult1.Subnets.First().DiscoveredDevices.ToDiffDevices(),
      ScanResult2.Subnets.First().DiscoveredDevices.ToDiffDevices(),
      nameof(NetworkScanResult),
      options,
      testLogger
    );

    // Assert
    //Print( diffs );
    var diffsAsJson = JsonConverter.Serialize( diffs );
    return Verify( diffsAsJson );
  }


  [Test]
  public Task IpAsKeySelectorTest() {
    // Arrange
    var options = new DiffOptions()
      .SetDiffTypesAll()
      .SetKeySelector<DiscoveredDevice>( device =>
        device.Get( AddressType.IpV4 ) ?? throw new InvalidOperationException( "Device has no IP address" ) )
      .SetKeySelector<SubnetScanResult>( subnet => subnet.CidrBlock.ToString() )
      .SetKeySelector<Port>( port => port.Value.ToString() );

    // Act
    var diffs = ObjectDiffEngine.Compare( ScanResult1, ScanResult2, nameof(NetworkScanResult), options );

    // Assert
    Print( diffs );
    // TODO custom serializer that skips properties not directly defined on the object (e.g. lists, objects)
    var diffsAsJson = JsonConverter.Serialize( diffs, new IpAddressConverter() );
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
    var diffs = ObjectDiffEngine.Compare( original, updated, nameof(NetworkScanResult), options );

    // Assert
    Print( diffs );
    var diffsAsJson = JsonConverter.Serialize( diffs );
    return Verify( diffsAsJson );
  }

  [Test]
  public Task UnchangedUsingKeySelectorTest() {
    // Arrange
    var options = new DiffOptions()
      .ConfigureDiffDeviceKeySelectors( [] )
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
    var diffs = ObjectDiffEngine.Compare( original, updated, nameof(NetworkScanResult), options );

    // Assert
    Print( diffs );
    var diffsAsJson = JsonConverter.Serialize( diffs );
    return Verify( diffsAsJson );
  }

  [Test]
  public Task SubListAddedTest() {
    // Arrange
    var options = new DiffOptions()
      .ConfigureDiffDeviceKeySelectors( [] )
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
    var diffs = ObjectDiffEngine.Compare( original, updated, nameof(NetworkScanResult), options );

    // Assert
    Print( diffs );
    var diffsAsJson = JsonConverter.Serialize( diffs );
    return Verify( diffsAsJson );
  }

  [Test]
  public Task SubListRemovedTest() {
    // Arrange
    var options = new DiffOptions()
      .ConfigureDiffDeviceKeySelectors( [] )
      .SetDiffTypesAll();

    var original = new List<DiscoveredDevice> {
      new() { Addresses = [new IpV4Address( "192.168.0.21" )], Ports = [22, 443, 80] }
    }.ToDiffDevices();

    var updated = new List<DiscoveredDevice> {
      new() { Addresses = [new IpV4Address( "192.168.0.21" )], Ports = [443, 80] }
    }.ToDiffDevices();

    // Act
    var diffs = ObjectDiffEngine.Compare( original, updated, nameof(NetworkScanResult), options );

    // Assert
    Print( diffs );
    var diffsAsJson = JsonConverter.Serialize( diffs );
    return Verify( diffsAsJson );
  }

  [Test]
  public void MatchDeclaredAndDiscoveredTest() {
    List<DeclaredDevice> declaredDevices = [
      new() {
        Addresses = [new HostnameAddress( "t14", IsId: true ), new MacAddress( "14-14-14-14-14-14", isId: false )]
      },
    ];
    List<DiscoveredDevice> discoveredDevices = [
      new() { Addresses = [new HostnameAddress( "t14" )] }
    ];

    var original = declaredDevices.ToDiffDevices();
    var updated = discoveredDevices.ToDiffDevices();

    var diffs = ObjectDiffEngine.Compare( original, updated, "Device",
      new DiffOptions { IgnorePaths = ["Device[*].Addresses[*].Required"] }
        .ConfigureDiffDeviceKeySelectors( declaredDevices )
        .SetDiffTypesAll()
    );

    Print( diffs );
    var diffsAsJson = JsonConverter.Serialize( diffs );
    Verify( diffsAsJson );
  }

  private static void Print( List<ObjectDiff> diffs ) {
    foreach ( var diff in diffs ) {
      Console.WriteLine( $"{diff.PropertyPath}: {diff.DiffType} — '{diff.Original}' → '{diff.Updated}'" );
    }
  }

  private sealed class IpAddressConverter : JsonConverter<System.Net.IPAddress> {
    public override System.Net.IPAddress Read( ref Utf8JsonReader reader, Type typeToConvert,
      JsonSerializerOptions options ) {
      string? ip = reader.GetString();
      var ipAddress = ( ip == null ) ? null : System.Net.IPAddress.Parse( ip );
      return ipAddress ?? throw new Exception( "Cannot read" ); //System.Net.IPAddress.None;
    }

    public override void Write( Utf8JsonWriter writer, System.Net.IPAddress value, JsonSerializerOptions options ) {
      ArgumentNullException.ThrowIfNull( writer );
      writer.WriteStringValue( value?.ToString() );
    }
  }
}