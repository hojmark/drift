using System.Text.Json;
using System.Text.Json.Serialization;
using Drift.Domain;

namespace Drift.Coordinator.Api.Control.JsonConverters;

internal sealed class CidrBlockJsonConverter : JsonConverter<CidrBlock> {
  public override CidrBlock Read( ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options ) {
    return new CidrBlock( reader.GetString() ?? throw new JsonException( "CIDR block cannot be null." ) );
  }

  public override void Write( Utf8JsonWriter writer, CidrBlock value, JsonSerializerOptions options ) {
    writer.WriteStringValue( value.ToString() );
  }
}