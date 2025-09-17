using Drift.Cli.Commands.Scan.Interactive.Models;
using Drift.Cli.Commands.Scan.Interactive.Simulation;
using Drift.Cli.Output;
using Drift.Cli.Scan;
using Drift.Cli.Tools;
using Drift.Domain.Scan;

namespace Drift.Cli.Commands.Scan.Interactive;

public static class NewScanUi {
  private static readonly ScanSession DemoScan1 = new(
    TimeSpan.FromSeconds( 10 ),
    [
      new Models.Subnet {
        Address = "192.168.1.0/24",
        Devices = [
          new Device { Ip = "192.168.1.10", Mac = "AA:BB:CC:DD:EE:01", IsOnline = true },
          new Device { Ip = "192.168.1.11", Mac = "AA:BB:CC:DD:EE:02", IsOnline = false },
          new Device { Ip = "192.168.1.12", Mac = "AA:BB:CC:DD:EE:03", IsOnline = true }
        ]
      },
      new Models.Subnet {
        Address = "10.0.0.0/24",
        Devices = [
          new Device { Ip = "10.0.0.1", Mac = "FF:EE:DD:CC:BB:01", IsOnline = true },
          new Device { Ip = "10.0.0.2", Mac = "FF:EE:DD:CC:BB:02", IsOnline = true }
        ]
      }
    ]
  );

  // TODO themes: greyscale, light, default

  public static async Task Show() {
    //var scanner = new SimulatedScanner( DemoScan1 );
    var scanner = new PingNetworkScanner( new NullOutputManager(), new OsPingTool() );
    var app = new ScanUiApp( scanner );
    await app.RunAsync();
  }
}