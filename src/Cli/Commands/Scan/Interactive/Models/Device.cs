namespace Drift.Cli.Commands.Scan.Interactive.Models;

internal class Device {
  // TODO get rid of the double fields
  public required string Ip {
    get;
    set;
  }

  public required string IpRaw {
    get;
    set;
  }

  public required string Mac {
    get;
    set;
  }

  public required string MacRaw {
    get;
    set;
  }
  
  public required string Id {
    get;
    set;
  }
  
  public required string IdRaw {
    get;
    set;
  }

  public required string Status {
    get;
    set;
  }

  public required string StatusText {
    get;
    set;
  }
}