using System.Net;

namespace Drift.Domain.Device.Addresses;

public interface IIpAddress {
  IPAddress Ip {
    get;
  }
}