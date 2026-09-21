using System.Text.Json;

namespace Drift.Coordinator.Services.Models;

public sealed record ScanEventData(
  long EventId,
  Guid ScanId,
  ScanStatus Status,
  byte Progress,
  JsonElement? Result = null
);