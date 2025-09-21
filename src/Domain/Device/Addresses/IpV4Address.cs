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

  public IpV4Address( string ipAddress, bool? isId = null ) {
    if ( !IPAddress.TryParse( ipAddress, out var ip ) || ip.AddressFamily != AddressFamily.InterNetwork ) {
      throw new ArgumentException( $"'{ipAddress}' is not a valid IPv4 address.", nameof(ipAddress) );
    }

    Value = ipAddress;
    IsId = isId;
  }
  
  public IpV4Address( IPAddress ipAddress, bool? isId = null ) {
    if ( ipAddress.AddressFamily != AddressFamily.InterNetwork ) {
      throw new ArgumentException( $"'{ipAddress}' is not a valid IPv4 address.", nameof(ipAddress) );
    }

    Value = ipAddress.ToString();
    IsId = isId;
  }
}