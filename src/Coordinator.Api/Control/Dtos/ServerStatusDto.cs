using System.Text.Json.Serialization;

namespace Drift.Coordinator.Api.Control.Dtos;

public sealed record ServerStatusDto(
  [property: JsonConverter( typeof(JsonStringEnumConverter<ServerStatusDtoValue>) )]
  ServerStatusDtoValue Status
);

public enum ServerStatusDtoValue {
  Ready
}