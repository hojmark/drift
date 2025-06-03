namespace Drift.TestUtilities.ResourceProviders;

public static class LocalTestResourceProvider {
  [Obsolete( "Prefer stream based method" )]
  public static string GetPath( string resource ) {
    return $"../../../resources/{resource}";
  }

  public static Stream GetStream( string resource ) {
    return new FileStream( $"../../../resources/{resource}", FileMode.Open );
  }
}