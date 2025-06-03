using YamlDotNet.Serialization;

namespace Drift.Domain.Device.Addresses;

// TODO maybe IpAddress interface implementation still make sense, but should then be called RoutableAddress(?)
[YamlSerializable]
public /*readonly*/ record HostnameAddress /*: IpAddress*/ : IDeviceAddress {
  public AddressType Type => AddressType.Hostname;

  public HostnameAddress( string Value, bool? IsId = null ) {
    this.Value = Value;
    this.IsId = IsId;
  }

  // For Yaml deserialization
  public HostnameAddress() {
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