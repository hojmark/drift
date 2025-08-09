using System.Net;
using System.Net.Sockets;

namespace Drift.Domain.Device.Addresses;

public struct IpV4Address : IIpAddress {
  public AddressType Type => AddressType.IpV4;

  public string Value {
    get;
  }

  public bool? IsId {
    get;
  }

  public IpV4Address( string value, bool? isId = null ) {
    if ( !IPAddress.TryParse( value, out var ip ) || ip.AddressFamily != AddressFamily.InterNetwork ) {
      throw new ArgumentException( $"'{value}' is not a valid IPv4 address.", nameof(value) );
    }

    Value = value;
    IsId = isId;
  }
}