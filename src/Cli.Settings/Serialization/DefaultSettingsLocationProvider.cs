using System.Runtime.InteropServices;

namespace Drift.Cli.Settings.Serialization;

internal sealed class DefaultSettingsLocationProvider : ISettingsLocationProvider {
  public string GetDirectory() {
    if ( RuntimeInformation.IsOSPlatform( OSPlatform.Windows ) ) {
      return Path.Combine( Environment.GetFolderPath( Environment.SpecialFolder.ApplicationData ), "Drift" );
    }

    if ( RuntimeInformation.IsOSPlatform( OSPlatform.Linux ) ) {
      return Path.Combine( Environment.GetFolderPath( Environment.SpecialFolder.UserProfile ), ".config", "drift" );
    }

    throw new PlatformNotSupportedException();
  }
}