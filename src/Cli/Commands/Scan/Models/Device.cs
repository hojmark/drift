using Drift.Cli.Presentation.Rendering;

namespace Drift.Cli.Commands.Scan.Models;

internal class Device {
  public required DisplayValue Ip {
    get;
    set;
  }

  public required DisplayValue Mac {
    get;
    set;
  }

  public required DisplayValue Id {
    get;
    set;
  }

  public required string State {
    get;
    set;
  }

  public required string StateText {
    get;
    set;
  }

  public string Note {
    get;
    set;
  } //TODO e.g. "Last seen 5 hours ago""
}