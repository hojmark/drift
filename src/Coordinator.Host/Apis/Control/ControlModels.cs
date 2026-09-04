namespace Drift.Coordinator.Host.Apis.Control;

public sealed record ServerStatusResponse( string Status );

public sealed record AgentSummary( string Id, string? Address, string Status );

public sealed record StartScanRequest( IReadOnlyCollection<string>? Cidrs = null, uint PingsPerSecond = 50 );

public sealed record StartScanResponse( string ScanId, string Status );

public sealed record ScanStatusResponse( string ScanId, string Status, byte Progress );

public sealed record ScanEvent( string ScanId, string Status, byte Progress, object? Result = null );