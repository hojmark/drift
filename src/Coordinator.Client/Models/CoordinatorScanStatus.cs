using System.Text.Json.Serialization;

namespace Drift.Coordinator.Client.Models;

[JsonConverter( typeof(JsonStringEnumConverter<CoordinatorScanStatus>) )]
public enum CoordinatorScanStatus {
  Queued,
  Running,
  Completed,
  Cancelled,
  Failed
}