using YamlDotNet.Serialization;

namespace Drift.Domain.Device.Addresses;

//TODO consider struct for value incl. validation. Like Cidr.
[YamlSerializable]
public /*readonly*/ record MacAddress : IDeviceAddress {
  public AddressType Type => AddressType.Mac;

  public MacAddress( string Value, bool? IsId = null ) {
    this.Value = Value;
    this.IsId = IsId;
  }

  // For Yaml deserialization
  public MacAddress() {
  }

  public string Value {
    get;
    set;
  }

  public bool? IsId {
    get;
    set;
  }
}