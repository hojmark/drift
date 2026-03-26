using Drift.Domain.Device.Addresses;

namespace Drift.Scanning.Oui;

/// <summary>
/// Provides MAC OUI (Organizationally Unique Identifier) vendor lookups.
/// The vendor dictionary is generated from the IEEE OUI CSV at https://standards-oui.ieee.org/oui/oui.csv
/// and lives in OuiDatabase.Generated.cs.
/// To refresh, run: nuke UpdateOui.
/// </summary>
public static partial class OuiDatabase {
  /// <summary>
  /// Looks up the vendor name for a given MAC address.
  /// </summary>
  /// <param name="mac">The MAC address to look up.</param>
  /// <returns>The vendor/organization name, or <c>null</c> if not found.</returns>
  public static string? LookupVendor( MacAddress mac ) {
    var value = mac.Value;

    // Parse the first 3 octets from positions 0-1, 3-4, 6-7
    if ( !TryParseHexByte( value, 0, out var b0 ) ||
         !TryParseHexByte( value, 3, out var b1 ) ||
         !TryParseHexByte( value, 6, out var b2 ) ) {
      return null;
    }

    var oui = ( (uint) b0 << 16 ) | ( (uint) b1 << 8 ) | b2;

    return _vendors.GetValueOrDefault( oui );
  }

  private static bool TryParseHexByte( string s, int offset, out byte result ) {
    result = 0;
    var hi = HexVal( s[offset] );
    var lo = HexVal( s[offset + 1] );
    if ( hi < 0 || lo < 0 ) {
      return false;
    }

    result = (byte) ( ( hi << 4 ) | lo );
    return true;
  }

  private static int HexVal( char c ) {
    return c switch {
      >= '0' and <= '9' => c - '0',
      >= 'A' and <= 'F' => c - 'A' + 10,
      >= 'a' and <= 'f' => c - 'a' + 10,
      _ => -1,
    };
  }
}