namespace Drift.Cli.Commands.Scan.Interactive.Simulation;

public class ScanSession {
  public TimeSpan Duration {
    get;
  }

  public List<Models.Subnet> Subnets {
    get;
  }

  public ScanSession( TimeSpan duration, List<Models.Subnet> subnets ) {
    Duration = duration;
    Subnets = subnets;
  }

  public int TotalDevices => Subnets.Sum( s => s.Devices.Count );
}