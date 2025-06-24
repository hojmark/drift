namespace Drift.Spec.Schema;

public static class SpecVersionExtensions {
  public static string ToJsonSchemaFileName( this SpecVersion version ) {
    return $"drift-spec-{version.ToJsonSchemaFileNameVersionPart()}.schema.json";
  }

  private static string ToJsonSchemaFileNameVersionPart( this SpecVersion version ) {
    return version.ToString().ToLowerInvariant().Replace( "_", "-" );
  }
}