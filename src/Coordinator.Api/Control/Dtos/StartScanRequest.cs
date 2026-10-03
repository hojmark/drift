using Drift.Domain;

namespace Drift.Coordinator.Api.Control.Dtos;

public sealed record StartScanRequest( IReadOnlyCollection<CidrBlock>? Cidrs = null, int PingsPerSecond = 50 );