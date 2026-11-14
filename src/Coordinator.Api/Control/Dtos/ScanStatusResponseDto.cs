namespace Drift.Coordinator.Api.Control.Dtos;

public sealed record ScanStatusResponseDto( Guid ScanId, ScanStatusDto Status, byte Progress );