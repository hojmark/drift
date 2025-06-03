using Drift.Domain.Device.Declared;
using Drift.Domain.Device.Discovered;

namespace Drift.Cli.Commands.Scan.Rendering;

internal class ScanRenderData {
  internal required IEnumerable<DiscoveredDevice> DevicesDiscovered {
    get;
    init;
  }

  internal required IEnumerable<DeclaredDevice> DevicesDeclared {
    get;
    init;
  } = [];
}