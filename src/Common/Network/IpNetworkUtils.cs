using System.Net;
using System.Net.Sockets;
using Drift.Domain;

namespace Drift.Common.Network;

// TODO use library for some of these implementations?
// TODO weak typing: ip address and subnet mask shares type
public static class IpNetworkUtils {
  /// <summary>
  /// Determines the IP <em>network</em> address (e.g. 192.168.0.0) based on an IP <em>host</em> address (e.g. 192.168.0.42) and subnet mask (e.g. 255.255.255.0).
  /// </summary>
  /// <param name="ip">The IP address to calculate the network address from (the host address).</param>
  /// <param name="mask">The subnet mask to apply.</param>
  /// <returns>
  /// An <see cref="IPAddress"/> representing the network (subnet) address, calculated by applying the subnet mask to the given IP address.
  /// </returns>
  public static IPAddress GetNetworkAddress( IPAddress ip, IPAddress mask ) {
    var ipBytes = ip.GetAddressBytes();
    var maskBytes = mask.GetAddressBytes();
    var subnetBytes = new byte[ipBytes.Length];

    for ( int i = 0; i < ipBytes.Length; i++ ) {
      subnetBytes[i] = (byte) ( ipBytes[i] & maskBytes[i] );
    }

    return new IPAddress( subnetBytes );
  }

  /// <summary>
  /// Calculates the CIDR (Classless Inter-Domain Routing) prefix length of a subnet mask.
  /// </summary>
  /// <param name="mask">The subnet mask.</param>
  /// <returns>The prefix length (i.e., the number of leading one-bits in the binary representation of the mask).</returns>
  public static int GetCidrPrefixLength( IPAddress mask ) {
    return mask.GetAddressBytes()
      // Count the number of bits set to 1 in each byte
      .Select( b => Convert.ToString( b, toBase: 2 ).Count( bit => bit == '1' ) )
      .Sum();
  }

  public static IPAddress GetNetmask( int prefixLength ) {
    return IPNetwork2.Parse( $"0.0.0.0/{prefixLength}" ).Netmask;
  }

  /// <summary>
  /// Checks if the specified IPv4 address belongs to a <em>Private Address Space</em> as defined by RFC1918.
  /// </summary>
  /// <param name="ip">The IPv4 address to evaluate.</param>
  /// <returns><c>true</c> if the address is private; otherwise, <c>false</c>.</returns>
  /// <remarks>
  /// Private IPv4 address ranges are:
  /// <code>
  /// 10.0.0.0 to 10.255.255.255 (10.0.0.0/8)
  /// 172.16.0.0 to 172.31.255.255 (172.16.0.0/12)
  /// 192.168.0.0 to 192.168.255.255 (192.168.0.0/16)
  /// </code>
  /// See alsohttps://www.rfc-editor.org/rfc/rfc1918.html#section-3.
  /// </remarks>
  public static bool IsPrivateIpV4( IPAddress ip ) {
    if ( ip.AddressFamily == AddressFamily.InterNetworkV6 ) {
      // Support should be possible to add... see RFC4193.
      throw new ArgumentException( "IPv6 addresses are not supported.", nameof(ip) );
    }

    if ( ip.AddressFamily != AddressFamily.InterNetwork ) {
      return false;
    }

    var ipBytes = ip.GetAddressBytes();

    return
      ipBytes[0] == 10 || // 10.0.0.0/8
      ( ipBytes[0] == 172 && ipBytes[1] >= 16 && ipBytes[1] <= 31 ) || // 172.16.0.0/12
      ( ipBytes[0] == 192 && ipBytes[1] == 168 ); // 192.168.0.0/16
  }

  /// <summary>
  /// Calculates the total number of IP addresses within a given subnet, based on the provided <see cref="CidrBlock"/>.
  /// </summary>
  /// <param name="cidr">The CIDR block.</param>
  /// <param name="usable">Specifies whether to exclude the network and broadcast addresses from the count. Defaults to true.</param>
  /// <returns>
  /// The total number of IP addresses in the subnet, adjusted for usability if specified.
  /// </returns>
  public static long GetIpRangeCount( CidrBlock cidr, bool usable = true ) {
    return (long) IPNetwork2
      .Parse( cidr.ToString() )
      .ListIPAddress( usable ? Filter.Usable : Filter.All )
      .Count;
  }
}