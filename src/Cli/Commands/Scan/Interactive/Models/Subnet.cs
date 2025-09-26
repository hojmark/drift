using Drift.Domain;

namespace Drift.Cli.Commands.Scan.Interactive.Models;

internal class Subnet {
  public required CidrBlock Cidr {
    get;
    set;
  }

  public required List<Device> Devices {
    get;
    init;
  }

  public bool IsExpanded {
    get;
    set;
  } = true;
}