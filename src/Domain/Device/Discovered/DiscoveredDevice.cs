using Drift.Domain.Device.Addresses;

namespace Drift.Domain.Device.Discovered;

public record DiscoveredDevice : IAddressableDevice {
  public List<IDeviceAddress> Addresses {
    get;
    init;
  } = [];

  public List<Port> Ports {
    get;
    init;
  } = [];

  public DateTime Timestamp {
    get;
    init;
  } // When was it observed?
}