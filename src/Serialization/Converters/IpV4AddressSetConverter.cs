using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization;
using Drift.Domain.Device.Addresses;

namespace Drift.Serialization.Converters;

public sealed class IpV4AddressSetConverter : JsonConverter<IReadOnlySet<IpV4Address>> {
  public override IReadOnlySet<IpV4Address>? Read( ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options ) {
    if ( reader.TokenType != JsonTokenType.StartArray ) {
      throw new JsonException( "Expected array" );
    }

    var builder = ImmutableHashSet.CreateBuilder<IpV4Address>();

    while ( reader.Read() ) {
      if ( reader.TokenType == JsonTokenType.EndArray ) {
        return builder.ToImmutable();
      }

      var ipAddress = JsonSerializer.Deserialize<string>( ref reader, options );
      if ( ipAddress != null ) {
        builder.Add( new IpV4Address( ipAddress ) );
      }
    }

    throw new JsonException( "Unexpected end of JSON" );
  }

  public override void Write( Utf8JsonWriter writer, IReadOnlySet<IpV4Address> value, JsonSerializerOptions options ) {
    writer.WriteStartArray();
    
    foreach ( var ip in value ) {
      JsonSerializer.Serialize( writer, ip.Value, options );
    }

    writer.WriteEndArray();
  }
}
