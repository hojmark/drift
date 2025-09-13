namespace Drift.Cli.Commands.Scan.Interactive.Simulation;

public class ScanSession {
  public TimeSpan Duration {
    get;
  }

  public List<Subnet> Subnets {
    get;
  }

  public ScanSession( TimeSpan duration, List<Subnet> subnets ) {
    Duration = duration;
    Subnets = subnets;
  }

  public int TotalDevices => Subnets.Sum( s => s.Devices.Count );
}