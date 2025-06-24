namespace Drift.Domain.Device.Addresses;

// TODO Validate with regex or library. Goes for other address types too.
public /*readonly*/ record IpV4Address( string Value, bool? IsId = null ) : IIpAddress {
  public AddressType Type => AddressType.IpV4;
}