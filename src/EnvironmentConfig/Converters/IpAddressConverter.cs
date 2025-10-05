using System.Text.Json;
using System.Text.Json.Serialization;

namespace Drift.EnvironmentConfig.Converters;

public sealed class IpAddressConverter : JsonConverter<System.Net.IPAddress> {
  public override System.Net.IPAddress Read(
    ref Utf8JsonReader reader,
    Type typeToConvert,
    JsonSerializerOptions options
  ) {
    string? ip = reader.GetString();
    var ipAddress = ( ip == null ) ? null : System.Net.IPAddress.Parse( ip );
    return ipAddress ?? throw new Exception( "Cannot read" ); // System.Net.IPAddress.None;
  }

  public override void Write( Utf8JsonWriter writer, System.Net.IPAddress value, JsonSerializerOptions options ) {
    ArgumentNullException.ThrowIfNull( writer );
    writer.WriteStringValue( value?.ToString() );
  }
}
