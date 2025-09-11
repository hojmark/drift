using Drift.Cli.Commands.Scan.Interactive.Simulation;

namespace Drift.Cli.Commands.Scan.Interactive;

public static class NewScanUi {
  public static void Show() {
    var subnets = Subnet.Sample(); // Or inject later
    var scanner = new SimulatedScanner( new ScanSession( "Test", TimeSpan.FromSeconds( 10 ), subnets ) );
    var app = new ScanUiApp( subnets );
    app.Run();
  }
}