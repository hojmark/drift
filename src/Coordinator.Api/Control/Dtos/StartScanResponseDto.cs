namespace Drift.Coordinator.Api.Control.Dtos;

public sealed record StartScanResponseDto( Guid ScanId, ScanStatusDto Status );