using YamlDotNet.Serialization;

namespace Drift.Domain.Device.Addresses;

[YamlSerializable]
public enum AddressType {
  IpV4 = 1,
  IpV6 = 2,
  Mac = 3,
  Hostname = 4
}