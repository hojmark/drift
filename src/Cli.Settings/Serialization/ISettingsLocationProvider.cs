namespace Drift.Cli.Settings.Serialization;

public interface ISettingsLocationProvider {
  internal const string SettingsFileName = "settings.json";

  string GetDirectory();

  string GetFile() {
    return Path.Combine( GetDirectory(), SettingsFileName );
  }
}