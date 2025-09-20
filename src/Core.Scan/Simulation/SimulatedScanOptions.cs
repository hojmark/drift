namespace Drift.Core.Scan.Simulation;

public class SimulatedScanOptions {
  public required TimeSpan Duration {
    get;
    init;
  }

  public required List<Models.Subnet> Subnets {
    get;
    init;
  }

  public int TotalDevices => Subnets.Sum( s => s.Devices.Count );
}