using Drift.Domain;
using Drift.Spec.Dtos.V1_preview;
using Drift.Spec.Dtos.V1_preview.Mappers;
using YamlDotNet.Serialization;

namespace Drift.Spec.Serialization;

public static partial class YamlConverter {
  // Consider Reader/Stream/etc

  public static string Serialize( Inventory network, bool jsonCompatible = false ) {
    var spec = Mapper.ToDto( network );
    return SerializeToDto( spec, jsonCompatible );
  }

  internal static string SerializeToDto( DriftSpec network, bool jsonCompatible = false ) {
    // TODO Add spacing to make it easier to read: https://github.com/aaubry/YamlDotNet/issues/803
    var builder = new StaticSerializerBuilder( new YamlStaticContext() )
      .ConfigureNamingConventions()
      .ConfigureDefaultValuesHandling( DefaultValuesHandling.OmitNull | DefaultValuesHandling.OmitEmptyCollections );

    if ( jsonCompatible ) {
      builder.JsonCompatible();
    }

    var serializer = builder.Build();

    return serializer.Serialize( network );
  }
}