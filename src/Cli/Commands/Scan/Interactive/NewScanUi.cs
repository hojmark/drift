using Drift.Cli.Commands.Scan.Interactive.Simulation;
using Drift.Cli.Scan;
using Drift.Cli.Tests.Utils;
using Drift.Cli.Tools;
using Drift.Domain.Scan;

namespace Drift.Cli.Commands.Scan.Interactive;

public static class NewScanUi {
  private static readonly ScanSession DemoScan1 = new(
    TimeSpan.FromSeconds( 10 ),
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
    ]
  );

  // TODO themes: greyscale, light, default

  public static async Task Show() {
    IScanService scanner = new SimulatedScanner( DemoScan1 );
    scanner = new PingNetworkScanner( new NullOutputManager(), new OsPingTool() );
    var app = new ScanUiApp( scanner );
    await app.RunAsync();
  }
}