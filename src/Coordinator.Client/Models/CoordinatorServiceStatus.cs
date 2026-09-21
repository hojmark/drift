using System.Text.Json.Serialization;

namespace Drift.Coordinator.Client.Models;

[JsonConverter( typeof(JsonStringEnumConverter<CoordinatorServiceStatus>) )]
public enum CoordinatorServiceStatus {
  Ready
}