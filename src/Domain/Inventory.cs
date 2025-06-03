using YamlDotNet.Serialization;

namespace Drift.Domain;

[YamlSerializable]
public record Inventory {
  public Network Network {
    get;
    set;
  }
}