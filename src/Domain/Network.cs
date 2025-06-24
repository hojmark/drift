using Drift.Domain.Device.Declared;

namespace Drift.Domain;

public record Network {
  public List<DeclaredSubnet> Subnets {
    get;
    set;
  } = [];

  public List<DeclaredDevice> Devices {
    get;
    set;
  } = [];
}