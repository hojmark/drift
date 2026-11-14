using System.Text.Json;
using System.Text.Json.Serialization;
using Drift.Domain;

namespace Drift.Serialization.Converters;

public sealed class AgentIdConverter : JsonConverter<AgentId> {
  public override AgentId Read( ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options ) {
    return AgentId.Parse(
      reader.GetString() ?? throw new JsonException( "Agent ID cannot be null." ),
      provider: null
    );
  }

  public override void Write( Utf8JsonWriter writer, AgentId value, JsonSerializerOptions options ) {
    writer.WriteStringValue( value.Value );
  }
}