using Drift.Domain;
using Drift.Domain.Device;
using Drift.Domain.Device.Addresses;

namespace Drift.Diff.Domain;

// TODO reintroduce outcommented plus configure diffengine:
// diffEngine.IgnoreProperty<DeviceForDiff>(d => d.Id);
// diffEngine.IgnoreProperty<DeviceForDiff>(d => d.Origin);
public record DiffDevice : IAddressableDevice {
  /*public string? Id {
    get;
    init;
  }*/

  public List<IDeviceAddress> Addresses {
    get;
    init;
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