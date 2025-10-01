using Drift.Cli.Presentation.Rendering;
using Drift.Cli.Presentation.Rendering.DeviceState;

namespace Drift.Cli.Commands.Scan.Models;

internal class Device {
  public required DisplayValue Ip {
    get;
    init;
  }

  public required DisplayValue Mac {
    get;
    init;
  }

  public required DisplayValue Id {
    get;
    init;
  }

  public required DeviceRenderState State {
    get;
    init;
  }

  public string Note {
    get;
    init;
  } //TODO e.g. "Last seen 5 hours ago""
}