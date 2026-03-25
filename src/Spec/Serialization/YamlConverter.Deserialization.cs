using System.Text;
using Drift.Domain;
using Drift.Spec.Dtos.V1_preview;
using Drift.Spec.Dtos.V1_preview.Mappers;
using YamlDotNet.Serialization;

namespace Drift.Spec.Serialization;

public static partial class YamlConverter {
  // Consider Reader/Stream/etc

  public static Inventory Deserialize( Stream stream ) {
    using var reader = new StreamReader( stream, Encoding.UTF8 );
    return Deserialize( reader.ReadToEnd() );
  }

  public static Inventory Deserialize( FileInfo fileInfo ) {
    using var stream = fileInfo.Open( FileMode.Open, FileAccess.Read, FileShare.Read );
    return Deserialize( stream );
  }

  internal static DriftSpec DeserializeToDto( string yaml ) {
    var deserializer = new StaticDeserializerBuilder( new YamlStaticContext() )
      .IgnoreUnmatchedProperties() // TODO remove
      .ConfigureNamingConventions()
      .Build();

    var spec = deserializer.Deserialize<DriftSpec?>( yaml ); // null when the YAML is an empty string

    return spec ?? new DriftSpec();
  }

  private static Inventory Deserialize( string yaml ) {
    var spec = DeserializeToDto( yaml );
    return Mapper.ToDomain( spec );
  }
}