namespace Drift.EnvironmentConfig;

internal static class Settings {
  internal static readonly string UserSettingsDirectory =
    Path.Combine( Environment.GetFolderPath( Environment.SpecialFolder.UserProfile ), ".config", "drift" );
}