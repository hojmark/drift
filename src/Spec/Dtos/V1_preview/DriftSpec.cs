using Drift.Spec.Dtos.V1_preview.Mappers;
using Json.Schema.Generation;

namespace Drift.Spec.Dtos.V1_preview;

[Title( "Drift spec schema" )]
[Description( "JSON schema for validating Drift specs" )]
[AdditionalProperties( false )] // TODO set to false
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

  // TODO support settings
  /*public Settings? Settings {
    get;
    set;
  }*/

  public List<Agent>? Agents {
    get;
    set;
  }
}

// [Title( "Network declaration" )]
[AdditionalProperties( false )] // TODO set to false
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

  [Required]
  public List<DeviceAddress> Addresses {
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

// TODO add patterns
[AdditionalProperties( false )]
public record DeviceAddress {
  [Required]
  public string Type {
    get;
    set;
  }

  [Required]
  public string Value {
    get;
    set;
  }

  public bool? IsId {
    get;
    set;
  }
}

[AdditionalProperties( false )]
public record Settings {
  public UnknownDevicePolicy? UnknownDevices {
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
  public string Id {
    get;
    set;
  }

  public string Address {
    get;
    set;
  }
}

public enum UnknownDevicePolicy {
  Disallowed = 1,
  Allowed = 2
}