using Drift.Spec.Dtos.V1_preview.Mappers;
using Json.Schema.Generation;

namespace Drift.Spec.Dtos.V1_preview;

[Title( "Drift spec schema" )]
[Description( "JSON schema for validating Drift specs" )]
[AdditionalProperties( false )]
// TODO rename to DriftSpecV1Preview?
public record DriftSpec {
  [property: Const(
    // Jusification: formatting issue
#pragma warning disable SA1114
    // TODO both spec and mapper should get version constant from elsewhere
    Mapper.VersionConstant
#pragma warning restore SA1114
  )]
  [property: Required]
  public string Version {
    get;
    set;
  }

  [Required]
  public Network Network {
    get;
    set;
  }

  public Server? Server {
    get;
    set;
  }

  public Settings? Settings {
    get;
    set;
  }

  public List<Agent>? Agents {
    get;
    set;
  }
}

// [Title( "Network declaration" )]
[AdditionalProperties( false )]
public record Network {
  public List<Subnet>? Subnets {
    get;
    set;
  }

  public List<Device>? Devices {
    get;
    set;
  }
}

/// <summary>
/// Declares where the coordinating server for this network is meant to run.
/// </summary>
[AdditionalProperties( false )]
public record Server {
  [Required]
  // TODO Use Uri type
  public string Address {
    get;
    set;
  }
}

[AdditionalProperties( false )]
public record Subnet {
  public string? Id {
    get;
    set;
  }

  [Required]
  public string Address {
    get;
    set;
  }

  public bool? Enabled {
    get;
    set;
  }
}

[AdditionalProperties( false )]
public record Device {
  public string? Id {
    get;
    set;
  }

  /// <summary>
  /// Gets or sets the addresses used to identify this device when matching declared vs.
  /// discovered state.
  /// </summary>
  [Required]
  public Addresses Addresses {
    get;
    set;
  }

  /// <summary>
  /// Gets or sets additional descriptive addresses that are not used for device identity.
  /// </summary>
  public Addresses? Info {
    get;
    set;
  }

  public DeviceState? State {
    get;
    set;
  }

  public bool? Enabled {
    get;
    set;
  }
}

public enum DeviceState {
  /// <summary>
  /// Device must always be up (online)
  /// </summary>
  Up = 1,

  /// <summary>
  /// Device can be up or down (no strict requirement)
  /// </summary>
  Dynamic = 2, // TODO less ambiguous name?

  /// <summary>
  /// Device should always be down (offline)
  /// </summary>
  Down = 3
}

/// <summary>
/// A set of addresses for a device.
/// </summary>
// TODO enforce "at least one property set"
[AdditionalProperties( false )]
public record Addresses {
  public string? Ipv4 {
    get;
    set;
  }

  public string? Mac {
    get;
    set;
  }

  public string? Hostname {
    get;
    set;
  }
}

[AdditionalProperties( false )]
public record Settings {
  /// <summary>
  /// Gets or sets whether a device discovered on the network but not declared in the spec is
  /// considered a spec violation (adherence to spec), or tolerated.
  /// </summary>
  public UnknownDevicePolicy? UnknownDevices {
    get;
    set;
  }

  /// <summary>
  /// Gets or sets the default policy for connections not explicitly declared by any agent's
  /// <c>policy:</c> block: whether such connections are expected to be reachable or blocked by
  /// default.
  /// </summary>
  public UndeclaredConnectionsPolicy? UndeclaredConnections {
    get;
    set;
  }

  public int? PingThrottling {
    get;
    set;
  }

  public bool? ScanOnlyDeclaredSubnets {
    get;
    set;
  }
}

[AdditionalProperties( false )]
public record Agent {
  [Required]
  public string Id {
    get;
    set;
  }

  [Required]
  public string Address {
    get;
    set;
  }

  public List<Policy>? Policy {
    get;
    set;
  }
}

/// <summary>
/// A single policy assertion executed by an agent against a target.
/// </summary>
// TODO extend `To`/`Port`/`Probe` to accept either a single value or an array
[AdditionalProperties( false )]
public record Policy {
  /// <summary>
  /// Gets or sets the target(s) of this policy assertion: a subnet id, device id, a
  /// well-known target (<c>internet</c>, <c>peers</c>, <c>rfc1918</c>), or a list thereof.
  /// </summary>
  [Required]
  public List<string> To {
    get;
    set;
  }

  // TODO lock down to an enum once the full set of probe types and their possible outcomes is
  // pinned down (e.g. "reachable"/"unreachable" for ICMP/TCP, "valid"/"invalid" for TLS).
  [Required]
  public string Expect {
    get;
    set;
  }

  public List<int>? Port {
    get;
    set;
  }

  // TODO lock down to an enum (e.g. tcp/udp/icmp) once probe execution is built.
  public string? Protocol {
    get;
    set;
  }

  // TODO lock down to an enum once extended probe types are defined.
  public List<string>? Probe {
    get;
    set;
  }

  // TODO lock down to an enum (currently only "gateway" is a known value).
  public string? Fallback {
    get;
    set;
  }
}

public enum UnknownDevicePolicy {
  Disallowed = 1,
  Allowed = 2
}

public enum UndeclaredConnectionsPolicy {
  Blocked = 1,
  Reachable = 2
}