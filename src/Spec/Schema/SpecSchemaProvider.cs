using Drift.Common.EmbeddedResources;

namespace Drift.Spec.Schema;

public static class SpecSchemaProvider {
  internal static string AsText( SpecVersion version ) {
    return EmbeddedResourceProvider.GetStream( GetPath( version ) ).ReadText();
  }

  private static string GetPath( SpecVersion version ) {
    return $"schemas/{version.ToJsonSchemaFileName()}";
  }
}