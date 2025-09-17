namespace Drift.Core.Scan.Device.Simulation.Models;

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