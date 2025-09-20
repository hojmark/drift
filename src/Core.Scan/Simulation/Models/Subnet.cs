namespace Drift.Core.Scan.Simulation.Models;

public class Subnet {
  public required string Address {
    get;
    set;
  }

  public required List<Device> Devices {
    get;
    set;
  } 
}