using Drift.Domain.Device.Addresses;

namespace Drift.Domain.Device;

public class DeviceId( List<IDeviceAddress> addresses ) {
  private List<IDeviceAddress> Addresses {
    get;
  } = addresses.Where( a => a.IsId ?? true ).ToList();

  public bool Contains( DeviceId other ) {
    //if ( other is null ) return false;
    if ( ReferenceEquals( this, other ) ) return true;

    // Local normalization to compare addresses reliably
    static string Normalize( string value ) {
      return string.IsNullOrWhiteSpace( value )
        ? string.Empty
        : value.ToUpperInvariant();
    }

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
  }

  /// <summary>
  /// considered the same if they share at least one type of address, and for every address type they have in common, the values are identical.
  /// </summary>
  /// <param name="other"></param>
  /// <returns></returns>
  public bool IsSame( DeviceId other ) {
    if ( other is null ) {
      return false;
    }

    if ( ReferenceEquals( this, other ) ) {
      return true;
    }

    // Local normalization to compare addresses reliably
    static string Normalize( string value ) {
      return string.IsNullOrWhiteSpace( value )
        ? string.Empty
        : value.ToUpperInvariant();
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
  }
}