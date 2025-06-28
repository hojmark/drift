namespace Drift.Spec.Schema;

public static class SpecVersionExtensions {
  public static string ToFileName( this SpecVersion version ) {
    return $"drift-spec-{version.ToFileNameVersionPart()}.schema.json";
  }

  private static string ToFileNameVersionPart( this SpecVersion version ) {
    return version.ToString().ToLowerInvariant().Replace( "_", "-" );
  }
}