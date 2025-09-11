using Drift.Cli.Commands.Scan.Interactive.Simulation;

namespace Drift.Cli.Commands.Scan.Interactive;

public static class NewScanUi {
  private static readonly ScanSession DemoScan1 = new("Test", TimeSpan.FromSeconds( 10 ),
  [
    new Subnet( "192.168.1.0/24",
    [
      new Device( "192.168.1.10", "AA:BB:CC:DD:EE:01", true ),
      new Device( "192.168.1.11", "AA:BB:CC:DD:EE:02", false ),
      new Device( "192.168.1.12", "AA:BB:CC:DD:EE:03", true )
    ] ),
    new Subnet( "10.0.0.0/24",
    [
      new Device( "10.0.0.1", "FF:EE:DD:CC:BB:01", true ),
      new Device( "10.0.0.2", "FF:EE:DD:CC:BB:02", true )
    ] )
  ]);

  public static void Show() {
    var scanner = new SimulatedScanner( DemoScan1 );
    var app = new ScanUiApp( scanner );
    app.Run();
  }
}