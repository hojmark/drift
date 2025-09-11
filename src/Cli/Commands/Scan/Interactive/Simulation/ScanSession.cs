namespace Drift.Cli.Commands.Scan.Interactive.Simulation;

public class ScanSession {
  public string Label {
    get;
  }

  public TimeSpan Duration {
    get;
  }

  public List<Subnet> Subnets {
    get;
  }

  public ScanSession( string label, TimeSpan duration, List<Subnet> subnets ) {
    Label = label;
    Duration = duration;
    Subnets = subnets;
  }

  public int TotalDevices => Subnets.Sum( s => s.Devices.Count );
}