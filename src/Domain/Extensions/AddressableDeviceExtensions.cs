using Drift.Domain.Device;
using Drift.Domain.Device.Addresses;

namespace Drift.Domain.Extensions;

public static class AddressableDeviceExtensions {
  //TODO return type should be nullable
  public static string GetSelector( this IAddressableDevice device ) {
    // TODO required only on declared? how about observed?
    /*return string.Join( "|", device.Addresses.OrderBy( a => a.Type ).Where( a =>
      // null equals required
      a.Required != false
    ).Select( a => $"{a.Type}:{a.Value}" ) );*/
    //TODO TEMP!!!
#pragma warning disable CS8603 // Possible null reference return.
    return device.Get( AddressType.IpV4 );
#pragma warning restore CS8603 // Possible null reference return.
  }

  public static string? Get( this IAddressableDevice device, AddressType addressType ) {
    return device.Addresses.SingleOrDefault( a => a.Type == addressType )?.Value;
  }
}