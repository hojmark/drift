using System.Runtime.InteropServices;

namespace Drift.Agent.Hosting.Identity;

public sealed class DefaultAgentIdentityLocationProvider : IAgentIdentityLocationProvider {
  public string GetDirectory() {
    if ( RuntimeInformation.IsOSPlatform( OSPlatform.Windows ) ) {
      return Path.Combine( Environment.GetFolderPath( Environment.SpecialFolder.ApplicationData ), "Drift", "agent" );
    }

    if ( RuntimeInformation.IsOSPlatform( OSPlatform.Linux ) ) {
      // https://specifications.freedesktop.org/basedir-spec/latest/
      var xdgConfigHome = Environment.GetEnvironmentVariable( "XDG_CONFIG_HOME" );

      var baseDir = string.IsNullOrEmpty( xdgConfigHome )
        ? Path.Combine( Environment.GetFolderPath( Environment.SpecialFolder.UserProfile ), ".config" )
        : xdgConfigHome;

      return Path.Combine( baseDir, "drift", "agent" );
    }

    throw new PlatformNotSupportedException();
  }
}
