using Drift.Domain.Device.Addresses;

namespace Drift.Domain.Device.Declared;

public record DeclaredDevice : IAddressableDevice {
  public string? Id {
    get;
    set;
  } = null;

  public required List<IDeviceAddress> Addresses {
    get;
    init;
  } = [];

  public bool? Enabled {
    get;
    set;
  } = true;

  public DeclaredDeviceState? State {
    get;
    set;
  } = DeclaredDeviceState.Up;

  public List<Port>? Ports {
    get;
    set;
  } = [];
}