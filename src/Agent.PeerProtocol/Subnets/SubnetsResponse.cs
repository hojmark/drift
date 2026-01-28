using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Drift.Domain;
using Drift.Networking.PeerStreaming.Core.Abstractions;
using Drift.Serialization.Converters;

namespace Drift.Agent.PeerProtocol.Subnets;

public sealed class SubnetsResponse : IPeerResponse {
  public static string MessageType => "subnets-response";

  public required IReadOnlyList<CidrBlock> Subnets {
    get;
    init;
  }

  public static JsonTypeInfo JsonInfo => SubnetsResponseJsonContext.Default.SubnetsResponse;
}

[JsonSourceGenerationOptions(
  Converters = [typeof(CidrBlockConverter), typeof(IpAddressConverter)],
  PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase
)]
[JsonSerializable( typeof(SubnetsResponse) )]
internal sealed partial class SubnetsResponseJsonContext : JsonSerializerContext;