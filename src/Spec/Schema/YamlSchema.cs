namespace Drift.Spec.Schema;

public static class YamlSchemaProvider {
  public static Stream AsStream( DriftSpecVersion version ) {
    return EmbeddedResourceProvider.GetStream( CreatePath( version ) );
  }

  public static string AsText( DriftSpecVersion version ) {
    return AsStream( version ).ReadText();
  }

  private static string CreatePath( DriftSpecVersion version ) {
    var versionString = version.ToString().ToLowerInvariant().Replace( "_", "-" );
    return $"schemas/drift-spec-{versionString}.schema.json";
  }
}