using System.Text.Json.Serialization;

namespace Drift.Coordinator.Api.Control.Dtos;

[JsonConverter( typeof(JsonStringEnumConverter<AgentConnectionStatusDto>) )]
public enum AgentConnectionStatusDto {
  Unknown,
  Connected,
  Unavailable
}