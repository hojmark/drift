using System.Text.Json;

namespace Drift.Coordinator.Api.Control.Dtos;

public sealed record ScanEventDto(
  long EventId,
  Guid ScanId,
  ScanStatusDto Status,
  byte Progress,
  JsonElement? Result = null
);