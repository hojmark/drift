namespace Drift.Coordinator.Services.Models;

public sealed record ScanStartResult( Guid ScanId, ScanStatus Status );