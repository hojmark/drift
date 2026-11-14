namespace Drift.Domain.Device.Addresses;

/// <summary>
/// Describes the set of known addresses for a discovered device.
/// At least one address must be provided; no single address type is universally required,
/// as different scan methods (ping, ARP, etc.) discover different address types.
/// </summary>
public sealed class DeviceAddressSet {
  public IpV4Address? Ip {
    get;
  }

  public MacAddress? Mac {
    get;
  }

  public HostnameAddress? Hostname {
    get;
  }

  public DeviceAddressSet( IpV4Address? ip = null, MacAddress? mac = null, HostnameAddress? hostname = null ) {
    if ( ip is null && mac is null && hostname is null ) {
      throw new ArgumentException( "At least one address must be provided." );
    }

    Ip = ip;
    Mac = mac;
    Hostname = hostname;
  }

  /// <summary>
  /// Converts to a flat address list suitable for <see cref="Drift.Domain.Device.Discovered.DiscoveredDevice.Addresses"/>.
  /// </summary>
  public List<IDeviceAddress> ToAddresses() {
    var list = new List<IDeviceAddress>();
    if ( Ip is not null ) {
      list.Add( Ip.Value );
    }

    if ( Mac is not null ) {
      list.Add( Mac.Value );
    }

    if ( Hostname is not null ) {
      list.Add( Hostname.Value );
    }

    return list;
  }
}