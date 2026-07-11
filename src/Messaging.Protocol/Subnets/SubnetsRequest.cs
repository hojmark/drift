using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Drift.Networking.Core.Abstractions;

namespace Drift.Messaging.Protocol.Subnets;

public sealed class SubnetsRequest : IRequest<SubnetsResponse> {
  public static string MessageType => "subnets-request";

  public static JsonTypeInfo JsonInfo => SubnetsRequestJsonContext.Default.SubnetsRequest;
}

[JsonSerializable( typeof(SubnetsRequest) )]
internal sealed partial class SubnetsRequestJsonContext : JsonSerializerContext;