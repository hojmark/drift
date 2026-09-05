using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;

namespace Drift.Coordinator.Host.Apis.Control;

[JsonSourceGenerationOptions( PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase )]
[JsonSerializable( typeof(ServerStatusResponse) )]
[JsonSerializable( typeof(AgentSummary[]) )]
[JsonSerializable( typeof(StartScanRequest) )]
[JsonSerializable( typeof(StartScanResponse) )]
[JsonSerializable( typeof(ScanStatusResponse) )]
[JsonSerializable( typeof(ScanEvent) )]
[JsonSerializable( typeof(ProblemDetails) )]
internal partial class ControlJsonSerializerContext : JsonSerializerContext;