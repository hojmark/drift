using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Drift.Spec.Serialization;

// TODO Split to generic yaml convert and InventoryConverter?
public static partial class YamlConverter {
  private static TBuilder ConfigureNamingConventions<TBuilder>( this StaticBuilderSkeleton<TBuilder> builder )
    where TBuilder : StaticBuilderSkeleton<TBuilder> {
    return builder
      .WithNamingConvention( UnderscoredNamingConvention.Instance )
      .WithEnumNamingConvention( HyphenatedNamingConvention.Instance );
  }
}