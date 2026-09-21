using System.Text.Json.Serialization;
using Drift.Domain.Scan;
using Drift.Serialization.Converters;

namespace Drift.Coordinator.Services.Scans;

#pragma warning disable SA1118
[JsonSourceGenerationOptions(
  PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
  Converters = [
    typeof(CidrBlockConverter),
    typeof(DeviceAddressConverter),
    typeof(IpV4AddressSetConverter),
    typeof(PercentageConverter),
    typeof(JsonStringEnumConverter<ScanResultStatus>)
  ] )]
#pragma warning restore SA1118
[JsonSerializable( typeof(NetworkScanResult) )]
internal sealed partial class ScanJsonSerializerContext : JsonSerializerContext;