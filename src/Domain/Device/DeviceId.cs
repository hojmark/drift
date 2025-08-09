using Drift.Domain.Device.Addresses;

namespace Drift.Domain.Device;

/// <summary>
/// A device ID may consist of one or more addresses, such as MAC, IPv4, IPv6, and/or hostname.
/// A discovered device (on the network) will only match a declared device (in the spec) if the device ID matches.
/// One or more addresses can be marked with <see cref="IDeviceAddress.IsId"/> <c>true</c> to indicate that it should contribute to the device ID.
/// </summary>
/// <param name="addresses">Addresses that potentially make up the device ID. Only addresses where <see cref="IDeviceAddress.IsId"/> is <c>true</c>  will be used</param>
public class DeviceId( List<IDeviceAddress> addresses ) {
  private List<IDeviceAddress> Addresses {
    get;
  } = addresses.Where( a => a.IsId != false ).ToList();

  public bool Contributes( AddressType type ) => Addresses.Any( a => a.Type == type && a.IsId != false );

  /// <summary>
  /// Determines whether this <see cref="DeviceId"/> is equivalent to another.
  /// Considered equivalent if both <see cref="DeviceId"/>s have at least one address type in common,
  /// and for every address type they have in common, values are identical.
  /// </summary>
  /// <seealso cref="op_Inequality"/>
  public static bool operator ==( DeviceId? left, DeviceId? right )
    => Equals( left, right );

  /// <summary>
  /// Determines whether this <see cref="DeviceId"/> is not equivalent to another.
  /// </summary>
  /// <seealso cref="op_Equality"/>
  public static bool operator !=( DeviceId? left, DeviceId? right )
    => !Equals( left, right );

  /*[Obsolete]
  public bool Contains( DeviceId other ) {
    //if ( other is null ) return false;
    if ( ReferenceEquals( this, other ) ) return true;

    var set = new HashSet<(AddressType Type, string Normalized)>();
    foreach ( var a in Addresses ) {
      set.Add( ( a.Type, Normalize( a.Value ) ) );
    }

    // If 'other' has no addresses, treat it as contained (empty set is subset of any set)
    // TODO each device should have at least one ideviceaddress
    var otherAddresses = other.Addresses;
    if ( otherAddresses.Count == 0 ) return true;

    foreach ( var b in otherAddresses ) {
      var key = ( b.Type, Normalize( b.Value ) );
      if ( !set.Contains( key ) ) return false;
    }

    return true;

    // Local normalization to compare addresses reliably
    static string Normalize( string value ) {
      return string.IsNullOrWhiteSpace( value )
        ? string.Empty
        : value.ToUpperInvariant();
    }
  }*/

  private bool IsSame( DeviceId other ) {
    if ( other is null ) {
      return false;
    }

    if ( ReferenceEquals( this, other ) ) {
      return true;
    }

    var thisLookup = Addresses.ToLookup( a => a.Type, a => Normalize( a.Value ) );
    var otherLookup = other.Addresses.ToLookup( a => a.Type, a => Normalize( a.Value ) );

    var commonTypes = thisLookup.Select( g => g.Key )
      .Intersect( otherLookup.Select( g => g.Key ) )
      .ToList();

    if ( !commonTypes.Any() ) {
      // No common address types to compare, so they can't be considered the same.
      return false;
    }

    // For all common address types, the values must be identical.
    return commonTypes.All( type => {
      var thisValues = new HashSet<string>( thisLookup[type] );
      var otherValues = new HashSet<string>( otherLookup[type] );
      return thisValues.SetEquals( otherValues );
    } );

    // Normalization to compare addresses reliably
    static string Normalize( string value ) {
      return string.IsNullOrWhiteSpace( value )
        ? string.Empty
        : value.ToUpperInvariant();
    }
  }

  public override bool Equals( object? obj ) {
    return IsSame( obj as DeviceId );
  }

  public override int GetHashCode() {
    return ToString().GetHashCode();
  }

  /// <summary>
  /// Returns a string representation of the <see cref="DeviceId"/>, consisting of the identifying addresses
  /// (those which has <see cref="IDeviceAddress.IsId"/> being <c>true</c>).
  /// If none are marked as identifiers, all addresses are included.
  /// </summary>
  /// <returns>
  /// A string representing the identifier for the device.
  /// </returns>
  public override string ToString() {
    var idAddresses = Addresses.Where( a => a.IsId == true ).ToList();
    var addressesToUse = idAddresses.Any() ? idAddresses : Addresses;

    return string.Join( "|", addressesToUse.OrderBy( a => a.Type ).Select( a => $"{a.Type}={a.Value}" ) );
  }
}