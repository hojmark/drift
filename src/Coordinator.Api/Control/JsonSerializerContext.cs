using System.Text.Json.Serialization;
using Drift.Coordinator.Api.Control.Dtos;
using Drift.Coordinator.Api.Control.JsonConverters;
using Drift.Serialization.Converters;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Drift.Coordinator.Api.Control;

[JsonSourceGenerationOptions(
  PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
#pragma warning disable SA1118
  Converters = [
    typeof(AgentIdConverter),
    typeof(CidrBlockJsonConverter),
    typeof(JsonStringEnumConverter<AgentEnrollmentStatusDto>),
    typeof(JsonStringEnumConverter<AgentConnectionStatusDto>),
    typeof(JsonStringEnumConverter<ScanStatusDto>)
  ] )]
#pragma warning restore SA1118
[JsonSerializable( typeof(AgentStateDto[]) )]
[JsonSerializable( typeof(AgentStateDto) )]
[JsonSerializable( typeof(EnrollAgentRequest) )]
[JsonSerializable( typeof(StartScanRequest) )]
[JsonSerializable( typeof(StartScanResponseDto) )]
[JsonSerializable( typeof(ScanStatusResponseDto) )]
[JsonSerializable( typeof(ScanEventDto) )]
[JsonSerializable( typeof(ServerStatusDto) )]
[JsonSerializable( typeof(ProblemDetails) )]
[JsonSerializable( typeof(HttpValidationProblemDetails) )]
internal sealed partial class JsonSerializerContext : System.Text.Json.Serialization.JsonSerializerContext;