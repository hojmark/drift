using Drift.Domain;
using Drift.Domain.Device.Addresses;
using Drift.Domain.Device.Discovered;
using NmapXmlParser;
using NmapRun = NmapXmlParser.nmaprun;
using NmapHost = NmapXmlParser.host;
using NmapAddress = NmapXmlParser.address;
using NmapPorts = NmapXmlParser.ports;
using NmapAddressType = NmapXmlParser.addressAddrtype;
using NmapHostnames = NmapXmlParser.hostnames;

namespace Drift.Parsers.NmapXml;

public static class NmapConverter {
  public static List<DiscoveredDevice> ToDevices( NmapRun nmapRun ) {
    return nmapRun.Items
      .Where( item => item is NmapHost )
      .Cast<NmapHost>()
      .Where( d => d.status.state != statusState.down ) // or up?
      .Select( MapDevice )
      .ToList();
  }

  private static DiscoveredDevice MapDevice( NmapHost host ) {
    return new DiscoveredDevice { Addresses = MapAddresses( host ), Ports = MapPorts( host ) };
  }

  private static List<Port> MapPorts( NmapHost host ) {
    return host.Items?
      .Where( item => item is NmapPorts )
      .Cast<NmapPorts>()
      .Where( item => item.port != null )
      .SelectMany( item => item.port )
      .Select( item => new Port( int.Parse( item.portid ) ) /*+ "/" + item.protocol */ )
      .ToList() ?? [];
  }

  private static List<IDeviceAddress> MapAddresses( NmapHost host ) {
    // Technically they can be here?
    var ipsAndMac = host.Items?
      .Where( item => item is NmapAddress )
      .Cast<NmapAddress>()
      .Select( MapAddress )
      .ToList() ?? [];

    var hostnames = host.Items?
      .Where( item => item is NmapHostnames )
      .Cast<NmapHostnames>()
      .Where( item => item.hostname != null )
      .SelectMany( item => item.hostname )
      .Select( item => (IDeviceAddress) new HostnameAddress( item.name /*+ " (" + item.type + ")"*/ ) )
      .ToList() ?? [];

    var deviceAddresses = ipsAndMac.Concat( hostnames ).ToList();
    deviceAddresses.Add( new IpV4Address( host.address.addr ) ); // TODO TEMP HACK

    return deviceAddresses;
  }

  // Consider using enum pattern matching instead of switch expression
  private static IDeviceAddress MapAddress( NmapAddress item ) {
    return item.addrtype switch {
      NmapAddressType.ipv4 => new IpV4Address( item.addr ),
      //TODO fix NmapAddressType.ipv6 => new IPv6Address( item.addr ),
      NmapAddressType.mac => new MacAddress( item.addr ),
      _ => throw new Exception( $"Unsupported address type: {item.addrtype}" )
    };
  }
}