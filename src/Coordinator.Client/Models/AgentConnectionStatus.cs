using System.Text.Json.Serialization;

namespace Drift.Coordinator.Client.Models;

[JsonConverter( typeof(JsonStringEnumConverter<AgentConnectionStatus>) )]
public enum AgentConnectionStatus {
  Unknown,
  Connected,
  Unavailable
}