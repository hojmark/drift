namespace Drift.Spec.Schema;

public static class SpecSchemaProvider {
  public static Stream AsStream( SpecVersion version ) {
    return EmbeddedResourceProvider.GetStream( CreatePath( version ) );
  }

  public static string AsText( SpecVersion version ) {
    return AsStream( version ).ReadText();
  }

  private static string CreatePath( SpecVersion version ) {
    var versionString = version.ToString().ToLowerInvariant().Replace( "_", "-" );
    return $"schemas/drift-spec-{versionString}.schema.json";
  }
}