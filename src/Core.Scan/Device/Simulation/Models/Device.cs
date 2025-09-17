namespace Drift.Core.Scan.Device.Simulation.Models;

public class Device {
  public required string Ip {
    get;
    set;
  }

  public required string Mac {
    get;
    set;
  }

  public required bool IsOnline {
    get;
    set;
  }
}