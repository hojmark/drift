using Drift.Domain.Device.Addresses;

namespace Drift.Domain.Device;

public interface IAddressableDevice {
  List<IDeviceAddress> Addresses {
    get;
    init;
  }
}