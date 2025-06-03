using System.Net;
using System.Net.Sockets;

namespace Drift.Domain;

/// <summary>
/// Represents a CIDR (Classless Inter-Domain Routing) block, which defines a range of IP addresses 
/// using a network address and a prefix length. For example, <c>192.168.1.0/24</c>.
/// </summary>
//TODO make obsolete
//[Obsolete( "Use .NET's IPNetwork instead" )]
public readonly struct CidrBlock {
  /// <summary>
  /// Gets the network address portion of the CIDR block.
  /// </summary>
  public IPAddress NetworkAddress {
    get;
  }

  /// <summary>
  /// Gets the prefix length of the CIDR block, indicating the number of leading 1-bits in the subnet mask.
  /// </summary>
  public int PrefixLength {
    get;
  }

  public CidrBlock( string cidrNotation ) {
    if ( string.IsNullOrWhiteSpace( cidrNotation ) )
      throw new ArgumentException( "CIDR notation cannot be null or empty.", nameof(cidrNotation) );

    var parts = cidrNotation.Trim().Split( '/' );
    if ( parts.Length != 2 )
      throw new FormatException( "Invalid CIDR format. Expected format: 'x.x.x.x/y'." );

    if ( !IPAddress.TryParse( parts[0], out var ip ) )
      throw new FormatException( "Invalid IP address in CIDR notation." );

    if ( !int.TryParse( parts[1], out int prefixLength ) )
      throw new FormatException( "Invalid prefix length in CIDR notation." );

    int maxPrefix = ip.AddressFamily switch {
      AddressFamily.InterNetwork => 32, // IPv4
      AddressFamily.InterNetworkV6 => 128, // IPv6
      _ => throw new NotSupportedException( "Only IPv4 and IPv6 are supported." )
    };

    if ( prefixLength < 0 || prefixLength > maxPrefix )
      throw new ArgumentOutOfRangeException( nameof(prefixLength),
        $"Prefix length must be between 0 and {maxPrefix} for {ip.AddressFamily} addresses." );

    NetworkAddress = ip;
    PrefixLength = prefixLength;
  }

  public override string ToString() => $"{NetworkAddress}/{PrefixLength}";
}