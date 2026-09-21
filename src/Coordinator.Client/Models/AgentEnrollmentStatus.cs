using System.Text.Json.Serialization;

namespace Drift.Coordinator.Client.Models;

[JsonConverter( typeof(JsonStringEnumConverter<AgentEnrollmentStatus>) )]
public enum AgentEnrollmentStatus {
  NotEnrolled,
  Enrolled
}