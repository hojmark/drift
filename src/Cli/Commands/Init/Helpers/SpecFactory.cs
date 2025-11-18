using Drift.Diff.Domain;
using Drift.Domain;
using Drift.Domain.Device.Addresses;
using Drift.Domain.Device.Declared;
using Drift.Domain.Extensions;
using Drift.Domain.Scan;
using NaturalSort.Extension;

namespace Drift.Cli.Commands.Init.Helpers;

internal static class SpecFactory {
  internal static void CreateFromScan(
    NetworkScanResult scanResult,
    string specPath
  ) {
    var subnets = scanResult.Subnets.Select( s => s.CidrBlock ).ToList();
    var devices = scanResult.Subnets.SelectMany( subnet => subnet.DiscoveredDevices ).ToDeclared();
    CreateSpec( subnets, devices, specPath );
  }

  // TODO consider "dropdown" for different templates
  internal static void CreateFromTemplate( string specPath ) {
    var builder = new NetworkBuilder();

    builder.AddSubnet( new CidrBlock( "192.168.1.0/24" ), id: "main-lan" );
    builder.AddSubnet( new CidrBlock( "192.168.100.0/24" ), id: "iot" );
    builder.AddSubnet( new CidrBlock( "192.168.200.0/24" ), id: "guest" );

    builder.AddDevice( [new IpV4Address( "192.168.1.10" )], id: "router", enabled: null, state: null );
    builder.AddDevice( [new IpV4Address( "192.168.1.20" )], id: "nas", enabled: null, state: null );
    builder.AddDevice( [new IpV4Address( "192.168.1.30" )], id: "server", enabled: null, state: null );
    builder.AddDevice( [new IpV4Address( "192.168.1.40" )], id: "desktop", enabled: null, state: null );
    builder.AddDevice( [new IpV4Address( "192.168.1.50" )], id: "laptop", enabled: null, state: null );

    builder.AddDevice( [new IpV4Address( "192.168.100.10" )], id: "smart-tv", enabled: null, state: null );
    builder.AddDevice( [new IpV4Address( "192.168.100.20" )], id: "security-camera", enabled: null, state: null );
    builder.AddDevice( [new IpV4Address( "192.168.100.30" )], id: "smart-switch", enabled: null, state: null );

    builder.AddDevice( [new IpV4Address( "192.168.200.100" )], id: "guest-device", enabled: null, state: null );

    builder.WriteToFile( specPath );
  }

  private static void CreateSpec(
    List<CidrBlock> subnets,
    List<DeclaredDevice> devices,
    string specPath
  ) {
    var networkBuilder = new NetworkBuilder();

    foreach ( var subnet in subnets ) {
      networkBuilder.AddSubnet( subnet );
    }

    var declaredDevices = devices
      .OrderBy( d => d.Get( AddressType.IpV4 ), StringComparison.OrdinalIgnoreCase.WithNaturalSort() );

    var no = 1;
    foreach ( var device in declaredDevices ) {
      networkBuilder.AddDevice( addresses: [..device.Addresses], id: $"device-{no++}", enabled: null, state: null );
    }

    networkBuilder.WriteToFile( specPath );
  }
}