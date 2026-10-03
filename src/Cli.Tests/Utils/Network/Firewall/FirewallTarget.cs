using System.Net;
using Drift.Domain;
using Drift.Domain.Device.Addresses;

namespace Drift.Cli.Tests.Utils.Network.Firewall;

/// <summary>
/// Represents a firewall target - can be a subnet name, IP address, CIDR block, or wildcard.
/// This is a discriminated union type providing type-safe firewall rule specification.
/// </summary>
public abstract record FirewallTarget {
  /// <summary>
  /// Create a firewall target from a subnet name (e.g., "dmz", "internal").
  /// </summary>
  public static FirewallTarget Subnet( string subnetName ) => new SubnetTarget( subnetName );

  /// <summary>
  /// Create a firewall target from an IP address.
  /// </summary>
  public static FirewallTarget FromIp( IpV4Address ip ) => new IpTarget( ip );

  /// <summary>
  /// Create a firewall target from a CIDR block (e.g., "192.168.1.0/24").
  /// </summary>
  public static FirewallTarget FromCidr( CidrBlock cidr ) => new CidrTarget( cidr );

  /// <summary>
  /// Wildcard target matching any source or destination.
  /// </summary>
  public static readonly FirewallTarget Any = new WildcardTarget();

  /// <summary>
  /// Check if this target matches a given subnet name and optional IP address.
  /// Used internally for firewall rule evaluation.
  /// </summary>
  internal abstract bool Matches( string subnetName, IpV4Address? ip );

  internal sealed record SubnetTarget( string Name ) : FirewallTarget {
    internal override bool Matches( string subnetName, IpV4Address? ip ) => Name == subnetName;

    public override string ToString() => Name;
  }

  internal sealed record IpTarget( IpV4Address Address ) : FirewallTarget {
    internal override bool Matches( string subnetName, IpV4Address? ip ) => ip.HasValue && Address.Equals( ip.Value );

    public override string ToString() => Address.Value;
  }

  internal sealed record CidrTarget( CidrBlock Block ) : FirewallTarget {
    internal override bool Matches( string subnetName, IpV4Address? ip ) {
      if ( ip == null ) {
        return false;
      }

      return IpInCidr( ip.Value, Block );
    }

    /// <summary>
    /// Check if an IP address is within a CIDR block.
    /// Since CidrBlock doesn't have a Contains() method, we implement our own.
    /// </summary>
    private static bool IpInCidr( IpV4Address ip, CidrBlock cidr ) {
      try {
        var ipBytes = IPAddress.Parse( ip.Value ).GetAddressBytes();
        var cidrParts = cidr.ToString().Split( '/' );
        if ( cidrParts.Length != 2 ) {
          return false;
        }

        var networkBytes = System.Net.IPAddress.Parse( cidrParts[0] ).GetAddressBytes();
        if ( !int.TryParse( cidrParts[1], out var prefixLength ) || prefixLength < 0 || prefixLength > 32 ) {
          return false;
        }

        var mask = ~0u << ( 32 - prefixLength );
        var ipInt = ( (uint) ipBytes[0] << 24 ) | ( (uint) ipBytes[1] << 16 ) | ( (uint) ipBytes[2] << 8 ) | ipBytes[3];
        var networkInt = ( (uint) networkBytes[0] << 24 ) | ( (uint) networkBytes[1] << 16 ) |
                         ( (uint) networkBytes[2] << 8 ) | networkBytes[3];

        return ( ipInt & mask ) == ( networkInt & mask );
      }
      catch {
        return false;
      }
    }

    public override string ToString() => Block.ToString();
  }

  internal sealed record WildcardTarget : FirewallTarget {
    internal override bool Matches( string subnetName, IpV4Address? ip ) => true;

    public override string ToString() => "*";
  }
}