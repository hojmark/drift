using System.Text.Json;
using System.Text.Json.Serialization;

namespace Drift.Cli.Settings.V1_preview.FeatureFlags;

internal sealed class FeatureFlagJsonConverter : JsonConverter<FeatureFlag> {
  public override FeatureFlag Read( ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options ) {
    return new(reader.GetString() ?? string.Empty);
  }

  public override void Write( Utf8JsonWriter writer, FeatureFlag value, JsonSerializerOptions options ) {
    writer.WriteStringValue( value.Name );
  }
}