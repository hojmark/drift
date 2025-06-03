using Drift.Domain;
using Drift.Domain.Device.Addresses;
using Drift.Domain.Device.Declared;
using Drift.Domain.Device.Discovered;
using Drift.Domain.Extensions;

namespace Drift.Diff.Domain;

public static class DeviceExtensions {
  //TODO doesn't belong in diffengine assembly
  public static DeclaredDevice ToDeclared( this DiscoveredDevice d ) => new() {
    Addresses = d.Addresses,
    Id = null,
    Type = null,
    State = DeclaredDeviceState.Up,
    Ports = null
  };

  public static List<DeclaredDevice> ToDeclared( this IEnumerable<DiscoveredDevice> devices ) =>
    devices.Select( ToDeclared ).ToList();

  private static DiffDevice ToDiffDevice( this DeclaredDevice d ) => new() {
    // Id = d.Id,
    Addresses = d.Addresses, Ports = d.Ports ?? [],
    // Origin = DeviceOrigin.Declared
  };

  private static DiffDevice ToDiffDevice( this DiscoveredDevice d ) => new() {
    Addresses = d.Addresses, Ports = d.Ports,
    // Origin = DeviceOrigin.Discovered
  };

  public static List<DiffDevice> ToDiffDevices( this IEnumerable<DeclaredDevice> devices ) =>
    devices.Select( ToDiffDevice ).ToList();

  public static List<DiffDevice> ToDiffDevices( this IEnumerable<DiscoveredDevice> devices ) =>
    devices.Select( ToDiffDevice ).ToList();

  public static DiffOptions ConfigureDiffDeviceKeySelectors( this DiffOptions diffOptions ) => diffOptions
    // TODO maybe the key selectors themselves should not be defined here
    .SetKeySelector<DiffDevice>( obj => obj.GetSelector() )
    .SetKeySelector<DeclaredDevice>( obj => obj.GetSelector() )
    .SetKeySelector<DiscoveredDevice>( obj => obj.GetSelector() )
    .SetKeySelector<Port>( obj => obj.Value.ToString() )
    //.SetKeySelector<IDeviceAddress>( obj => obj.Value.ToString() );
    .SetKeySelector<IDeviceAddress>(
      // Using 'Type' because the scope is the device (IDeviceAddress) and only one address of each type is allowed per device
      obj => obj.Type.ToString()
    );
}