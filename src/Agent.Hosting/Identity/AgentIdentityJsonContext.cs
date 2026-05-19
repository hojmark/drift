using System.Text.Json;
using System.Text.Json.Serialization;

namespace Drift.Agent.Hosting.Identity;

[JsonSourceGenerationOptions(
  ReadCommentHandling = JsonCommentHandling.Skip,
  PropertyNameCaseInsensitive = true,
  PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
  WriteIndented = true,
  DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
  RespectRequiredConstructorParameters = true,
  RespectNullableAnnotations = true
)]
[JsonSerializable( typeof(AgentIdentity) )]
internal sealed partial class AgentIdentityJsonContext : JsonSerializerContext {
}
