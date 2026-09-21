using System.Text.Json.Serialization;

namespace Drift.Coordinator.Api.Control.Dtos;

[JsonConverter( typeof(JsonStringEnumConverter<AgentEnrollmentStatusDto>) )]
public enum AgentEnrollmentStatusDto {
  NotEnrolled,
  Enrolled
}