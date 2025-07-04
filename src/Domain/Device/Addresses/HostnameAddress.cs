namespace Drift.Domain.Device.Addresses;

// TODO maybe IpAddress interface implementation still make sense, but should then be called RoutableAddress(?)
public /*readonly*/ record HostnameAddress /*: IpAddress*/( string Value, bool? IsId = null ) : IDeviceAddress {
  public AddressType Type => AddressType.Hostname;
}