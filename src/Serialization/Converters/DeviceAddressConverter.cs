using System.Text.Json;
using System.Text.Json.Serialization;
using Drift.Domain.Device.Addresses;

namespace Drift.Serialization.Converters;

public sealed class DeviceAddressConverter : JsonConverter<IDeviceAddress> {
  public override IDeviceAddress? Read( ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options ) {
    using var doc = JsonDocument.ParseValue( ref reader );
    var root = doc.RootElement;

    if ( !root.TryGetProperty( "type", out var typeProperty ) ) {
      throw new JsonException( "Missing 'type' property in IDeviceAddress JSON" );
    }

    var addressType = (AddressType) typeProperty.GetInt32();
    var value = root.GetProperty( "value" ).GetString()!;
    var isId = root.TryGetProperty( "isId", out var isIdProperty ) ? isIdProperty.GetBoolean() : (bool?) null;

    return addressType switch {
      AddressType.IpV4 => new IpV4Address( value, isId ),
      AddressType.Mac => new MacAddress( value, isId ),
      AddressType.Hostname => new HostnameAddress( value, isId ),
      _ => throw new JsonException( $"Unknown AddressType: {addressType}" )
    };
  }

  public override void Write( Utf8JsonWriter writer, IDeviceAddress value, JsonSerializerOptions options ) {
    writer.WriteStartObject();
    writer.WriteNumber( "type", (int) value.Type );
    writer.WriteString( "value", value.Value );
    if ( value.IsId.HasValue ) {
      writer.WriteBoolean( "isId", value.IsId.Value );
    }

    writer.WriteEndObject();
  }
}
