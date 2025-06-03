using Drift.Domain;
using Drift.Domain.Device;
using Drift.Domain.Device.Addresses;
using YamlDotNet.Serialization;

namespace Drift.Diff.Domain;

[YamlSerializable]
//TODO reintroduce outcommented plus configure diffengine:
//diffEngine.IgnoreProperty<DeviceForDiff>(d => d.Id);
//diffEngine.IgnoreProperty<DeviceForDiff>(d => d.Origin);
public record DiffDevice : IAddressableDevice {
  /*public string? Id {
    get;
    init;
  }*/

  public List<IDeviceAddress> Addresses {
    get;
    set;
  } = [];

  public List<Port> Ports {
    get;
    set;
  } = [];

  /* public DeviceOrigin Origin {
     get;
     init;
   } // Declared or Discovered*/
}