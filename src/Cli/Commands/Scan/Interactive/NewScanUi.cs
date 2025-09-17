using Drift.Core.Scan;
using Drift.Core.Scan.Device.Simulation;
using Drift.Core.Scan.Device.Simulation.Models;
using Drift.Domain.Scan;

namespace Drift.Cli.Commands.Scan.Interactive;

public static class NewScanUi {
  private static readonly SimulatedScanOptions DemoScan1 = new() {
    Duration = TimeSpan.FromSeconds( 10 ),
    Subnets = [
      new Subnet {
        Address = "192.168.1.0/24",
        Devices = [
          new Device { Ip = "192.168.1.10", Mac = "AA:BB:CC:DD:EE:01", IsOnline = true },
          new Device { Ip = "192.168.1.11", Mac = "AA:BB:CC:DD:EE:02", IsOnline = false },
          new Device { Ip = "192.168.1.12", Mac = "AA:BB:CC:DD:EE:03", IsOnline = true }
        ]
      },
      new Subnet {
        Address = "10.0.0.0/24",
        Devices = [
          new Device { Ip = "10.0.0.1", Mac = "FF:EE:DD:CC:BB:01", IsOnline = true },
          new Device { Ip = "10.0.0.2", Mac = "FF:EE:DD:CC:BB:02", IsOnline = true }
        ]
      }
    ]
  };

  // TODO themes: greyscale, light, default

  public static async Task Show( NetworkScanOptions scanRequest ) {
    //var scanner = new SimulatedScanner( DemoScan1 );
    var scanner = new PingNetworkScanner( new OsPingTool() );
    var app = new InteractiveScanUi( scanner );
    await app.RunAsync( scanRequest );
  }
}