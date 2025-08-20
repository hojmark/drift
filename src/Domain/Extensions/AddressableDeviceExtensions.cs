using Drift.Domain.Device;
using Drift.Domain.Device.Addresses;

namespace Drift.Domain.Extensions;

public static class AddressableDeviceExtensions {
  public static string GetDiffSelector( this IAddressableDevice device ) {
    return device.GetDeviceId().ToString();
  }

  public static string? Get( this IAddressableDevice device, AddressType addressType ) {
    return device.Addresses.SingleOrDefault( a => a.Type == addressType )?.Value;
  }
}