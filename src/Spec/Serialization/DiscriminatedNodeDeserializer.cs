using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace Drift.Spec.Serialization;

// PolymorphicNodeDeserializer?
public class DiscriminatedNodeDeserializer<TNode> : INodeDeserializer {
  private readonly Dictionary<string, Func<Dictionary<string, string>, object>> _typeMapping;
  private readonly INodeDeserializer _wrapped;
  private readonly string _discriminatorField;

  public DiscriminatedNodeDeserializer(
    INodeDeserializer wrapped,
    string discriminatorField,
    Dictionary<string, Func<Dictionary<string, string>, object>> typeMapping
  ) {
    _wrapped = wrapped;
    _discriminatorField = discriminatorField;
    _typeMapping = typeMapping;
  }

  public bool Deserialize(
    IParser reader,
    Type expectedType,
    Func<IParser, Type, object?> nestedObjectDeserializer,
    out object? value,
    ObjectDeserializer rootDeserializer
  ) {
    // Console.WriteLine( $"Deserializing expectedType: {expectedType}" );

    if ( expectedType != typeof(TNode) ) {
      return _wrapped.Deserialize( reader, expectedType, nestedObjectDeserializer, out value, rootDeserializer );
    }

    if ( reader.Current is not MappingStart ) {
      value = null;
      return false;
    }

    reader.MoveNext(); // Move past MappingStart event

    // Read mapping node into a dictionary (case-insensitive)
    var mapping = new Dictionary<string, string>( StringComparer.OrdinalIgnoreCase );

    while ( reader.Current is Scalar key ) {
      reader.MoveNext();
      if ( reader.Current is Scalar val ) {
        mapping[key.Value] = val.Value;
        reader.MoveNext();
      }
    }

    var typeToDeserialize = mapping[_discriminatorField];

    _typeMapping.TryGetValue( typeToDeserialize, out var typeMapping );

    if ( typeMapping == null ) {
      throw new YamlException( "Missing type mapping for type: " + typeToDeserialize );
    }

    value = typeMapping( mapping ); // TODO better error handling

    /*if ( !mapping.TryGetValue( "type", out var type ) )
      throw new InvalidOperationException( "Missing 'type' field" );

    if ( !mapping.TryGetValue( "value", out var addressValue ) )
      throw new InvalidOperationException( "Missing 'value' field" );

    // Note: unrecognized nodes skipped

    value = ( type.ToLowerInvariant() switch {
      "ipv4" => new IPv4Address( addressValue ),
      "mac" => new MacAddress( addressValue ),
      _ => throw new InvalidOperationException( $"Unknown type discriminator '{type}'" )
    } );*/

    reader.MoveNext(); // Move past  MappingEnd event
    return true;
  }
}