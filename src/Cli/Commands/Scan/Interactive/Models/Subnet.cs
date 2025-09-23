namespace Drift.Cli.Commands.Scan.Interactive.Models;

internal class Subnet {
  public required string Address {
    get;
    set;
  }

  public required List<Device> Devices {
    get;
    set;
  }
  
  public bool IsExpanded { get; set; } = true;
}