using Drift.Domain;

namespace Drift.Coordinator.Services.Models;

public sealed record StartScanCommand( IReadOnlyCollection<CidrBlock>? Cidrs = null, uint PingsPerSecond = 50 );