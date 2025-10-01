namespace Drift.Domain.Device.Addresses;

public readonly record struct HostnameAddress( string Value, bool? IsId = null ) : IDeviceAddress {
  public AddressType Type => AddressType.Hostname;
}