using Drift.Domain.Device.Declared;
using Drift.Domain.Device.Discovered;

namespace Drift.Cli.Presentation.Rendering.DeviceState;

internal static class DeviceStateResolver {
  internal static DeviceState Get(
    DeclaredDeviceState? declared,
    DiscoveredDeviceState? discovered,
    bool unknownAllowed
  ) {
    var isUnknown = declared == null;

    // Known device
    if ( !isUnknown ) {
      return declared switch {
        // expecting up, is up
        DeclaredDeviceState.Up when discovered == DiscoveredDeviceState.Online =>
          DeviceState.KnownExpectedOnline,
        // expecting up, is down
        DeclaredDeviceState.Up when discovered == DiscoveredDeviceState.Offline =>
          DeviceState.KnownUnexpectedOffline,
        // expecting down, is up
        DeclaredDeviceState.Down when discovered == DiscoveredDeviceState.Online =>
          DeviceState.KnownUnexpectedOnline,
        // expecting down, is down
        DeclaredDeviceState.Down when discovered == DiscoveredDeviceState.Offline =>
          DeviceState.KnownExpectedOffline,
        // expecting either, is up
        DeclaredDeviceState.Dynamic when discovered == DiscoveredDeviceState.Online =>
          DeviceState.KnownDynamicOnline,
        // expecting either, is down
        DeclaredDeviceState.Dynamic when discovered == DiscoveredDeviceState.Offline =>
          DeviceState.KnownDynamicOffline,
        // Error
        _ => DeviceState.Undefined
      };
    }

    // Unknown device
    if ( isUnknown && !unknownAllowed )
      return DeviceState.UnknownDisallowed;
    if ( isUnknown && unknownAllowed )
      return DeviceState.UnknownAllowed;

    // Fallback/Undefined
    //TODO log?
    return DeviceState.Undefined;
  }
}