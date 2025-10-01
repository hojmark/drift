namespace Drift.Domain.Device.Addresses;

public record HostnameAddress( string Value, bool? IsId = null ) : IDeviceAddress {
  public AddressType Type => AddressType.Hostname;
}