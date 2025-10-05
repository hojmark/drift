using Drift.Spec.Dtos.V1_preview;
using YamlDotNet.Serialization;

namespace Drift.Spec.Serialization;

[YamlStaticContext]
// TODO rely on attributes on the individual types instead?
[YamlSerializable( typeof(DriftSpec) )]
[YamlSerializable( typeof(Network) )]
[YamlSerializable( typeof(Subnet) )]
[YamlSerializable( typeof(Device) )]
[YamlSerializable( typeof(DeviceState) )]
[YamlSerializable( typeof(DeviceAddress) )]
[YamlSerializable( typeof(Agent) )]
/*[YamlSerializable( typeof(IpV4Address) )]
[YamlSerializable( typeof(HostnameAddress) )]
[YamlSerializable( typeof(MacAddress) )]
[YamlSerializable( typeof(Port) )]
[YamlSerializable( typeof(AddressType) )]*/
public partial class YamlStaticContext : StaticContext {
}