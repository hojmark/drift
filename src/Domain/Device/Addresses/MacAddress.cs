namespace Drift.Domain.Device.Addresses;

//TODO consider struct for value incl. validation. Like Cidr.
public /*readonly*/ record MacAddress( string Value, bool? IsId = null ) : IDeviceAddress {
  public AddressType Type => AddressType.Mac;
}