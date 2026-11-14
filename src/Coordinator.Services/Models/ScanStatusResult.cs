namespace Drift.Coordinator.Services.Models;

public sealed record ScanStatusResult( Guid ScanId, ScanStatus Status, byte Progress );