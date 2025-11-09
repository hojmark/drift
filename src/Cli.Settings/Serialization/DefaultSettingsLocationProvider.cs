using System.Runtime.InteropServices;

namespace Drift.Cli.Settings.Serialization;

internal sealed class DefaultSettingsLocationProvider : ISettingsLocationProvider {
  public string GetDirectory() {
    if ( RuntimeInformation.IsOSPlatform( OSPlatform.Windows ) ) {
      return Path.Combine( Environment.GetFolderPath( Environment.SpecialFolder.ApplicationData ), "Drift" );
    }

    if ( RuntimeInformation.IsOSPlatform( OSPlatform.Linux ) ) {
      // https://specifications.freedesktop.org/basedir-spec/latest/
      var xdgConfigHome = Environment.GetEnvironmentVariable( "XDG_CONFIG_HOME" );

      var baseDir = string.IsNullOrEmpty( xdgConfigHome )
        ? Path.Combine( Environment.GetFolderPath( Environment.SpecialFolder.UserProfile ), ".config" )
        : xdgConfigHome;

      return Path.Combine( baseDir, "drift" );
    }

    throw new PlatformNotSupportedException();
  }
}