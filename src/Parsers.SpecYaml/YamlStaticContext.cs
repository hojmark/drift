using Drift.Domain;
using Drift.Domain.Device.Addresses;
using Drift.Domain.Device.Declared;
using YamlDotNet.Serialization;

namespace Drift.Parsers.SpecYaml;

[YamlStaticContext]
//TODO rely on attributes on the individual types instead - or split domain and yaml types
[YamlSerializable( typeof(Inventory) )]
[YamlSerializable( typeof(Network) )]
[YamlSerializable( typeof(DeclaredSubnet) )]
[YamlSerializable( typeof(DeclaredDevice) )]
[YamlSerializable( typeof(DeclaredDeviceState) )]
[YamlSerializable( typeof(IpV4Address) )]
[YamlSerializable( typeof(HostnameAddress) )]
[YamlSerializable( typeof(MacAddress) )]
[YamlSerializable( typeof(Port) )]
[YamlSerializable( typeof(AddressType) )]
public partial class YamlStaticContext : YamlDotNet.Serialization.StaticContext {
}