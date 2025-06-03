using Drift.Domain.Device.Addresses;
using YamlDotNet.Serialization;

namespace Drift.Domain.Device.Declared;

[YamlSerializable]
public record DeclaredDevice : IAddressableDevice {
  public string? Id {
    get;
    //TODO should be init
    set;
  } = null;

  public string? Type {
    get;
    set;
  } = default!; // e.g., "host", "switch"

  public bool? Enabled {
    get;
    set;
  } = true;

  public DeclaredDeviceState? State {
    get;
    set;
  } = DeclaredDeviceState.Up;

  public List<IDeviceAddress> Addresses {
    get;
    set;
  } = [];

  public List<Port>? Ports {
    get;
    set;
  } = [];
}