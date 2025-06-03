using YamlDotNet.Serialization;

namespace Drift.Domain.Device.Addresses;

// TODO Validate with regex or library. Goes for other address types too.
//TODO consider struct for value incl. validation. Like Cidr.
[YamlSerializable]
public /*readonly*/ record IpV4Address : IIpAddress {
  public AddressType Type => AddressType.IpV4;

  public IpV4Address( string Value, bool? IsId = null ) {
    this.Value = Value;
    this.IsId = IsId;
  }

  // For Yaml deserialization
  public IpV4Address() {
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