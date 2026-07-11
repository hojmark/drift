using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Drift.Domain.Scan;
using Drift.Networking.Core.Abstractions;
using Drift.Serialization.Converters;

namespace Drift.Messaging.Protocol.Scan;

public sealed class ScanSubnetCompleteResponse : IResponse {
  public static string MessageType => "scan-complete";

  public required SubnetScanResult Result {
    get;
    init;
  }

  public static JsonTypeInfo JsonInfo => ScanSubnetCompleteResponseJsonContext.Default.ScanSubnetCompleteResponse;
}

[JsonSourceGenerationOptions(
  Converters = [typeof(CidrBlockConverter), typeof(IpAddressConverter), typeof(DeviceAddressConverter), typeof(IpV4AddressSetConverter)],
  PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase
)]
[JsonSerializable( typeof(ScanSubnetCompleteResponse) )]
internal sealed partial class ScanSubnetCompleteResponseJsonContext : JsonSerializerContext;
