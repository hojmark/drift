using System.Text.Json;
using System.Text.Json.Serialization;
using Drift.Domain;

namespace Drift.EnvironmentConfig.Converters;

public sealed class CidrBlockConverter : JsonConverter<CidrBlock> {
  public override CidrBlock Read(
    ref Utf8JsonReader reader,
    Type typeToConvert,
    JsonSerializerOptions options
  ) {
    var cidrString = reader.GetString();
    if ( string.IsNullOrEmpty( cidrString ) ) {
      throw new JsonException( "CIDR block string cannot be null or empty" );
    }

    return new CidrBlock( cidrString );
  }

  public override void Write( Utf8JsonWriter writer, CidrBlock value, JsonSerializerOptions options ) {
    ArgumentNullException.ThrowIfNull( writer );
    writer.WriteStringValue( value.ToString() );
  }
}