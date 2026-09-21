using System.Text.Json;
using System.Text.Json.Serialization;
using Drift.Domain;

namespace Drift.Serialization.Converters;

public sealed class PercentageConverter : JsonConverter<Percentage> {
  public override Percentage Read( ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options ) {
    return new Percentage( reader.GetByte() );
  }

  public override void Write( Utf8JsonWriter writer, Percentage value, JsonSerializerOptions options ) {
    writer.WriteNumberValue( value.Value );
  }
}