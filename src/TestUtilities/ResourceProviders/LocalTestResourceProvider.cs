namespace Drift.TestUtilities.ResourceProviders;

public static class LocalTestResourceProvider {
  public static Stream GetStream( string resource ) {
    return new FileStream( $"../../../resources/{resource}", FileMode.Open );
  }
}