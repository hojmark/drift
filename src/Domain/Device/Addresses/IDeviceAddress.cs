namespace Drift.Domain.Device.Addresses;

public interface IDeviceAddress {
  AddressType Type {
    get;
  }

  string Value {
    get;
  }

  bool? IsId {
    get;
  } // = true;
}