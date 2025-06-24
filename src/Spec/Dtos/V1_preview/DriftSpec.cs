using Json.Schema.Generation;

namespace Drift.Spec.Dtos.V1_preview;

[Title( "Drift spec schema" )]
[Description( "JSON schema for validating Drift specs" )]
[AdditionalProperties( false )] //TODO set to false
public record DriftSpec(
  // TODO make required const
  //[property: Const( "v1-preview" )]
  //[property: Required]
  string Version,
  Network Network,
  Settings? Settings );

[Title( "Network declaration" )]
[AdditionalProperties( false )] //TODO set to false
public record Network(
  [property: Title( "Network ID" )]
  [property: Description( "Unique identifier for the top-level network (may span multiple subnets)" )]
  string? Id,
  [property: Required] List<Subnet> Subnets,
  List<Device> Devices );

[AdditionalProperties( false )]
public record Subnet( string? Id, [property: Required] string Address, bool? Enabled );

[AdditionalProperties( false )]
public record Device( string? Id, List<DeviceAddress> Addresses, DeviceState? State, bool? Enabled );

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

[AdditionalProperties( false )]
public record DeviceAddress( string Type, string Value, bool? IsId );

[AdditionalProperties( false )]
public record Settings( UnknownDevicePolicy? UnknownDevices, int? PingThrottling );

public enum UnknownDevicePolicy {
  Disallowed = 1,
  Allowed = 2
}