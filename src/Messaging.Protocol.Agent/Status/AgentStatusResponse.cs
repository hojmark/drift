using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Drift.Networking.Core.Abstractions;

namespace Drift.Messaging.Protocol.Agent.Status;

public sealed class AgentStatusResponse : IResponse {
  public static string MessageType => "agent-status-response";

  public required AgentStatus Status {
    get;
    init;
  }

  public static JsonTypeInfo JsonInfo => AgentStatusResponseJsonContext.Default.AgentStatusResponse;
}

[JsonConverter( typeof(JsonStringEnumConverter<AgentStatus>) )]
public enum AgentStatus {
  Ready
}

[JsonSourceGenerationOptions( PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase )]
[JsonSerializable( typeof(AgentStatusResponse) )]
internal sealed partial class AgentStatusResponseJsonContext : JsonSerializerContext;