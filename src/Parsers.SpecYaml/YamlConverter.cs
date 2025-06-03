using System.Text;
using Drift.Domain;
using Drift.Domain.Device.Addresses;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using YamlDotNet.Serialization.NodeDeserializers;

namespace Drift.Parsers.SpecYaml;

//TODO ADD VERSION PROPERTY TO YAML "version: 1" (should it use semver or just an incrementing number?)
// TODO Split to generic yaml convert and InventoryConverter?
public static class YamlConverter {
  // Consider Reader/Stream/etc

  public static Network Deserialize( Stream stream ) {
    return Deserialize( new StreamReader( stream, Encoding.UTF8 ).ReadToEnd() );
  }

  public static Network Deserialize( FileInfo fileInfo ) {
    return Deserialize( fileInfo.Open( FileMode.Open, FileAccess.Read, FileShare.Read ) );
  }

  public static Network Deserialize( string yaml ) {
    var addressTypeMap =
      new Dictionary<string, Func<Dictionary<string, string>, object>>( StringComparer.OrdinalIgnoreCase ) {
        {
          //TODO use HyphenatedNamingConvention.Instance.Apply(nameof(AddressType.IpV4)) instead of hardcoded string
          "ip-v4", map => {
            map.TryGetValue( "required", out var required );
            return new IpV4Address( map["value"], bool.Parse( required ?? true.ToString() ) );
          }
        }, {
          "mac", map => {
            map.TryGetValue( "required", out var required );
            return new MacAddress( map["value"], bool.Parse( required ?? true.ToString() ) );
          }
        }, {
          "hostname", map => {
            map.TryGetValue( "required", out var required );
            return new HostnameAddress( map["value"], bool.Parse( required ?? true.ToString() ) );
          }
        }
      }; //TODO FUNC<(typeval,othervals),obj>

    var deserializer = new StaticDeserializerBuilder( new YamlStaticContext() )
      .WithNodeDeserializer<INodeDeserializer>(
        wrapped => new DiscriminatedNodeDeserializer<IDeviceAddress>( wrapped, "type", addressTypeMap ),
        s => s.InsteadOf<ObjectNodeDeserializer>() )
      /*.WithTypeDiscriminatingNodeDeserializer( o => {
        //TODO move to IDeviceAddress?
        o.AddKeyValueTypeDiscriminator<IDeviceAddress>( "type",
          new Dictionary<string, Type>( StringComparer.Ordinal ) { ["ipv4"] = typeof(IPv4Address), } );
      } )*/
      .IgnoreUnmatchedProperties() //TODO remove
      .ConfigureNamingConventions()
      .Build();

    return deserializer.Deserialize<Inventory>( yaml ).Network;
  }

  public static string Serialize( Inventory network ) {
    // Add spacing to make it easier to read: https://github.com/aaubry/YamlDotNet/issues/803
    var serializer = new StaticSerializerBuilder( new YamlStaticContext() )
      .ConfigureNamingConventions()
      .Build();

    return serializer.Serialize( network );
  }

  private static TBuilder ConfigureNamingConventions<TBuilder>( this StaticBuilderSkeleton<TBuilder> builder )
    where TBuilder : StaticBuilderSkeleton<TBuilder> {
    return builder
      .WithNamingConvention( UnderscoredNamingConvention.Instance )
      .WithEnumNamingConvention( HyphenatedNamingConvention.Instance );
  }


  //PolymorphicNodeDeserializer?
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
      IParser parser,
      Type expectedType,
      Func<IParser, Type, object?> nestedObjectDeserializer,
      out object? value,
      ObjectDeserializer rootDeserializer
    ) {
      //Console.WriteLine( $"Deserializing expectedType: {expectedType}" );

      if ( expectedType != typeof(TNode) ) {
        return _wrapped.Deserialize( parser, expectedType, nestedObjectDeserializer, out value, rootDeserializer );
      }

      if ( parser.Current is not MappingStart ) {
        value = null;
        return false;
      }

      parser.MoveNext(); // Move past MappingStart event

      // Read mapping node into a dictionary (case-insensitive)
      var mapping = new Dictionary<string, string>( StringComparer.OrdinalIgnoreCase );

      while ( parser.Current is Scalar key ) {
        parser.MoveNext();
        if ( parser.Current is Scalar val ) {
          mapping[key.Value] = val.Value;
          parser.MoveNext();
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

      parser.MoveNext(); // Move past  MappingEnd event
      return true;
    }
  }
}