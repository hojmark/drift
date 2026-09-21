using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Drift.Networking.Core.Abstractions;

namespace Drift.Messaging.Protocol.Agent.Status;

public sealed class AgentStatusRequest : IRequest<AgentStatusResponse> {
  public static string MessageType => "agent-status-request";

  public static JsonTypeInfo JsonInfo => AgentStatusRequestJsonContext.Default.AgentStatusRequest;
}

[JsonSerializable( typeof(AgentStatusRequest) )]
internal sealed partial class AgentStatusRequestJsonContext : JsonSerializerContext;