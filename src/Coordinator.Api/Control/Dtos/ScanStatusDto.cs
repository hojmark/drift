using System.Text.Json.Serialization;

namespace Drift.Coordinator.Api.Control.Dtos;

[JsonConverter( typeof(JsonStringEnumConverter<ScanStatusDto>) )]
public enum ScanStatusDto {
  Queued,
  Running,
  Completed,
  Cancelled,
  Failed
}