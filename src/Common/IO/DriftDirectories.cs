using System.Runtime.InteropServices;

namespace Drift.Common.IO;

public static class DriftDirectories {
  public static string ConfigDirectory {
    get {
      if ( RuntimeInformation.IsOSPlatform( OSPlatform.Windows ) ) {
        return Path.Combine( Environment.GetFolderPath( Environment.SpecialFolder.ApplicationData ), "Drift" );
      }

      if ( RuntimeInformation.IsOSPlatform( OSPlatform.Linux ) ) {
        var xdgConfigHome = Environment.GetEnvironmentVariable( "XDG_CONFIG_HOME" );
        var baseDirectory = string.IsNullOrEmpty( xdgConfigHome )
          ? Path.Combine( Environment.GetFolderPath( Environment.SpecialFolder.UserProfile ), ".config" )
          : xdgConfigHome;
        return Path.Combine( baseDirectory, "drift" );
      }

      throw new PlatformNotSupportedException();
    }
  }

  public static string DriftDataDirectory {
    get {
      if ( RuntimeInformation.IsOSPlatform( OSPlatform.Windows ) ) {
        return Path.Combine( Environment.GetFolderPath( Environment.SpecialFolder.LocalApplicationData ), "Drift" );
      }

      if ( RuntimeInformation.IsOSPlatform( OSPlatform.Linux ) ) {
        var xdgDataHome = Environment.GetEnvironmentVariable( "XDG_DATA_HOME" );
        var baseDirectory = string.IsNullOrEmpty( xdgDataHome )
          ? Path.Combine( Environment.GetFolderPath( Environment.SpecialFolder.UserProfile ), ".local", "share" )
          : xdgDataHome;
        return Path.Combine( baseDirectory, "drift" );
      }

      throw new PlatformNotSupportedException();
    }
  }
}