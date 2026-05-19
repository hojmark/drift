using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Drift.Domain;
using Drift.Networking.PeerStreaming.Core.Abstractions;
using Drift.Serialization.Converters;

namespace Drift.Agent.PeerProtocol.Scan;

public sealed class ScanSubnetRequest : IPeerRequest<ScanSubnetCompleteResponse> {
  public static string MessageType => "scan-subnet-request";

  public required CidrBlock Cidr {
    get;
    init;
  }

  public uint PingsPerSecond {
    get;
    init;
  } = 50;

  public static JsonTypeInfo JsonInfo => ScanSubnetRequestJsonContext.Default.ScanSubnetRequest;
}

[JsonSourceGenerationOptions(
  Converters = [typeof(CidrBlockConverter), typeof(IpAddressConverter)],
  PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase
)]
[JsonSerializable( typeof(ScanSubnetRequest) )]
internal sealed partial class ScanSubnetRequestJsonContext : JsonSerializerContext;
