namespace Drift.Coordinator.Services.Models;

public enum ScanStatus {
  Queued,
  Running,
  Completed,
  Cancelled,
  Failed
}