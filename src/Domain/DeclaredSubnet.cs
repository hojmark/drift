namespace Drift.Domain;

public record DeclaredSubnet {
  public string? Id {
    get;
    set;
  }

  /// <summary>
  /// Gets network address in CIDR notation.
  /// </summary>
  /// TODO change string -> Cidr
  public required string Address {
    get;
    init;
  } // e.g., "10.0.0.0/24"

  /*public string? Gateway {
    get;
    set;
  } // e.g., "10.0.0.1"*/

  /*public int Vlan {
    get;
    init;
  }*/

  public bool? Enabled {
    get;
    set;
  } = true;

  // TODO should be strongly typed i.e. DeviceId
  // Really needed? Can tell subnet from ip, but of course, if that's wrong...
  /*public List<string>? Devices {
    get;
    set;
  } = [];*/
}