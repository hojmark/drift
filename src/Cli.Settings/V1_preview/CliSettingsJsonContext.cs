using System.Text.Json;
using System.Text.Json.Serialization;
using Drift.Cli.Settings.V1_preview.Appearance;
using Drift.Cli.Settings.V1_preview.FeatureFlags;

namespace Drift.Cli.Settings.V1_preview;

[JsonSourceGenerationOptions(
  ReadCommentHandling = JsonCommentHandling.Skip,
  PropertyNameCaseInsensitive = true,
  PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
  WriteIndented = true,
  DefaultIgnoreCondition = JsonIgnoreCondition.Never,
  Converters = [typeof(CamelCaseJsonStringEnumConverter<OutputFormatSetting>), typeof(FeatureFlagJsonConverter)],
  // Recommended to enable the below two options: https://learn.microsoft.com/en-us/dotnet/api/system.text.json.jsonserializeroptions.respectrequiredconstructorparameters?view=net-9.0#remarks
  RespectRequiredConstructorParameters = true,
  RespectNullableAnnotations = true
)]
[JsonSerializable( typeof(CliSettings) )]
internal partial class CliSettingsJsonContext : JsonSerializerContext {
}

public class CamelCaseJsonStringEnumConverter<TEnum>()
  : JsonStringEnumConverter<TEnum>( JsonNamingPolicy.CamelCase ) where TEnum : struct, Enum;