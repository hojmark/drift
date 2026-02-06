using System.Runtime.InteropServices;

namespace Drift.TestUtilities;

// TODO DUPLICATE: move to shared project
public static class Environment {
  public static bool IsCi() {
    return System.Environment.GetEnvironmentVariable( "CI" ) == "true";
  }

  public static void SkipIfNot( Platform platform ) {
    var expectedOs = platform switch {
      Platform.Linux => OSPlatform.Linux,
      Platform.Windows => OSPlatform.Windows,
      _ => throw new PlatformNotSupportedException()
    };

    if ( !RuntimeInformation.IsOSPlatform( expectedOs ) ) {
      Assert.Inconclusive( $"Can only be run on {platform}" );
    }
  }
}