namespace Drift.Cli.Commands.Scan.Interactive.Models;

internal class Device {
  public required string Ip {
    get;
    set;
  }

  public required string Mac {
    get;
    set;
  }

  public required bool IsOnline {
    get;
    set;
  }
}