namespace Drift.Cli.Commands.Scan.Interactive.Models;

internal class UiSubnet {
  public Subnet Subnet {
    get;
  }

  public bool IsExpanded {
    get;
    set;
  }

  public UiSubnet( Subnet subnet, bool isExpanded = true ) {
    Subnet = subnet;
    IsExpanded = isExpanded;
  }
}