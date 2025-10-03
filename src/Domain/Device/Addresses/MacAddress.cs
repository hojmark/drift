using System.Text.RegularExpressions;

namespace Drift.Domain.Device.Addresses;

public readonly record struct MacAddress : IDeviceAddress {
  // Note: supports either colons or dashes
  private static readonly Regex MacRegex = new("^([0-9A-Fa-f]{2}([-:]?)){5}[0-9A-Fa-f]{2}$", RegexOptions.Compiled);

  public string Value {
    get;
  }

  public bool? IsId {
    get;
  }

  public AddressType Type => AddressType.Mac;

  public MacAddress( string value, bool? isId = null ) {
    if ( string.IsNullOrWhiteSpace( value ) ) {
      throw new ArgumentException( "MAC address cannot be empty.", nameof(value) );
    }

    var normalized = value.Trim().ToUpperInvariant().Replace( ":", "-" );

    if ( !MacRegex.IsMatch( normalized ) ) {
      throw new ArgumentException( $"Invalid MAC address format: '{value}'", nameof(value) );
    }

    Value = normalized;
    IsId = isId;
  }
}