using System.Runtime.InteropServices;

namespace Drift.Cli.Infrastructure;

internal static class Bootstrapper {
  internal static Task BootstrapAsync() {
    /*if ( !RuntimeInformation.IsOSPlatform( OSPlatform.Linux ) ) {
      throw new Exception( "Only Linux is supported" );
    }*/

    return Task.CompletedTask;
  }
}