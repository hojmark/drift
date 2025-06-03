using Drift.Domain.Device.Declared;
using YamlDotNet.Serialization;

namespace Drift.Domain;

[YamlSerializable]
public record Network {
  // Move to inventory maybe?
  public string Id {
    get;
    set;
  }

  public List<DeclaredSubnet> Subnets {
    get;
    set;
  } = [];

  public List<DeclaredDevice> Devices {
    get;
    set;
  } = [];
}