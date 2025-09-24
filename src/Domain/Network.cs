using Drift.Domain.Device.Declared;

namespace Drift.Domain;

public record Network {
  // TODO required?
  public NetworkId? Id {
    get;
    set;
  }

  public List<DeclaredSubnet> Subnets {
    get;
    set;
  } = [];

  public List<DeclaredDevice> Devices {
    get;
    set;
  } = [];
}