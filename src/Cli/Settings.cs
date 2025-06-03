namespace Drift.Cli;

internal static class Settings {
  internal static readonly string UserSettingsDirectory =
    Path.Combine( Environment.GetFolderPath( Environment.SpecialFolder.UserProfile ), ".config", "drift" );
}