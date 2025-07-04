using System.Text;
using Drift.Domain;
using Drift.Spec.Dtos.V1_preview;
using Drift.Spec.Dtos.V1_preview.Mappers;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Drift.Spec.Serialization;

// TODO Split to generic yaml convert and InventoryConverter?
public static class YamlConverter {
  // Consider Reader/Stream/etc

  public static Inventory Deserialize( Stream stream ) {
    return Deserialize( new StreamReader( stream, Encoding.UTF8 ).ReadToEnd() );
  }

  public static Inventory Deserialize( FileInfo fileInfo ) {
    return Deserialize( fileInfo.Open( FileMode.Open, FileAccess.Read, FileShare.Read ) );
  }

  public static Inventory Deserialize( string yaml ) {
    var spec = DeserializeToDto( yaml );
    return Mapper.ToDomain( spec );
  }

  internal static DriftSpec? DeserializeToDto( string yaml ) {
    var deserializer = new StaticDeserializerBuilder( new YamlStaticContext() )
      /*.WithTypeDiscriminatingNodeDeserializer( o => {
        //TODO move to IDeviceAddress?
        o.AddKeyValueTypeDiscriminator<IDeviceAddress>( "type",
          new Dictionary<string, Type>( StringComparer.Ordinal ) { ["ipv4"] = typeof(IPv4Address), } );
      } )*/
      .IgnoreUnmatchedProperties() //TODO remove
      .ConfigureNamingConventions()
      .Build();

    return deserializer.Deserialize<DriftSpec?>( yaml ); // ? because it may be null if the yaml is an empty string
  }

  public static string Serialize( Inventory network, bool jsonCompatible = false ) {
    var spec = Mapper.ToDto( network );
    return SerializeToDto( spec, jsonCompatible );
  }

  internal static string SerializeToDto( DriftSpec network, bool jsonCompatible = false ) {
    //TODO Add spacing to make it easier to read: https://github.com/aaubry/YamlDotNet/issues/803
    var builder = new StaticSerializerBuilder( new YamlStaticContext() )
      .ConfigureNamingConventions()
      .ConfigureDefaultValuesHandling( DefaultValuesHandling.OmitNull | DefaultValuesHandling.OmitEmptyCollections );

    if ( jsonCompatible ) {
      builder.JsonCompatible();
    }

    var serializer = builder.Build();

    return serializer.Serialize( network );
  }

  private static TBuilder ConfigureNamingConventions<TBuilder>( this StaticBuilderSkeleton<TBuilder> builder )
    where TBuilder : StaticBuilderSkeleton<TBuilder> {
    return builder
      .WithNamingConvention( UnderscoredNamingConvention.Instance )
      .WithEnumNamingConvention( HyphenatedNamingConvention.Instance );
  }
}