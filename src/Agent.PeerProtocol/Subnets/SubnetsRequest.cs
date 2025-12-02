using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Drift.Networking.PeerStreaming.Core.Abstractions;

namespace Drift.Agent.PeerProtocol.Subnets;

public sealed class SubnetsRequest : IPeerRequestMessage<SubnetsResponse> {
  public static string MessageType => "subnetsrequest";

  public static JsonTypeInfo JsonInfo => SubnetsRequestJsonContext.Default.SubnetsRequest;
}

[JsonSerializable( typeof(SubnetsRequest) )]
internal sealed partial class SubnetsRequestJsonContext : JsonSerializerContext;