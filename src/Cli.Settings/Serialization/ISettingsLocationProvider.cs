namespace Drift.Cli.Settings.Serialization;

public interface ISettingsLocationProvider {
  string GetDirectory();

  string GetFile() {
    return Path.Combine( GetDirectory(), "settings.json" );
  }
}