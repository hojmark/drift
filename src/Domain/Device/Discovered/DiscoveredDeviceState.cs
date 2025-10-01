namespace Drift.Domain.Device.Discovered;

public enum DiscoveredDeviceState {
  Online,

  // A discovered device can't really be offline, but it is convenient to have this state
  Offline,
  //Unreachable,
  //Unknown
}