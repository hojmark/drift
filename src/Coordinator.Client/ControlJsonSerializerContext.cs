using System.Text.Json.Serialization;
using Drift.Coordinator.Client.Models;
using Drift.Domain.Scan;
using Drift.Serialization.Converters;

namespace Drift.Coordinator.Client;

#pragma warning disable SA1118
[JsonSourceGenerationOptions(
  PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
  Converters = [
    typeof(CidrBlockConverter),
    typeof(DeviceAddressConverter),
    typeof(AgentIdConverter),
    typeof(IpV4AddressSetConverter),
    typeof(PercentageConverter),
    typeof(JsonStringEnumConverter<ScanResultStatus>)
  ] )]
#pragma warning restore SA1118
[JsonSerializable( typeof(AgentEnrollmentResult) )]
[JsonSerializable( typeof(CoordinatorAgentStatus[]) )]
[JsonSerializable( typeof(CoordinatorStatus) )]
[JsonSerializable( typeof(CoordinatorScanStatus) )]
[JsonSerializable( typeof(CoordinatorServiceStatus) )]
[JsonSerializable( typeof(ProblemDetailsResponse) )]
[JsonSerializable( typeof(ScanStartResponse) )]
[JsonSerializable( typeof(ScanEventResponse) )]
[JsonSerializable( typeof(NetworkScanResult) )]
internal sealed partial class ControlJsonSerializerContext : JsonSerializerContext;
