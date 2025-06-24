using YamlDotNet.Serialization;

namespace Drift.Spec.Serialization;

[YamlStaticContext]
//TODO rely on attributes on the individual types instead?
[YamlSerializable( typeof(Dtos.V1_preview.DriftSpec) )]
[YamlSerializable( typeof(Dtos.V1_preview.Network) )]
[YamlSerializable( typeof(Dtos.V1_preview.Subnet) )]
[YamlSerializable( typeof(Dtos.V1_preview.Device) )]
[YamlSerializable( typeof(Dtos.V1_preview.DeviceState) )]
[YamlSerializable( typeof(Dtos.V1_preview.DeviceAddress) )]
/*[YamlSerializable( typeof(IpV4Address) )]
[YamlSerializable( typeof(HostnameAddress) )]
[YamlSerializable( typeof(MacAddress) )]
[YamlSerializable( typeof(Port) )]
[YamlSerializable( typeof(AddressType) )]*/
public partial class YamlStaticContext : YamlDotNet.Serialization.StaticContext {
}