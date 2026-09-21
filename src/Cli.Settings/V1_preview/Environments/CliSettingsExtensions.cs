namespace Drift.Cli.Settings.V1_preview.Environments;

public static class CliSettingsExtensions {
  public static bool TryGetEnvironment( this CliSettings settings, string name, out EnvironmentSetting? environment ) {
    environment = settings.Environments.Find( e => e.Name == name );
    return environment != null;
  }

  public static EnvironmentSetting? GetActiveEnvironment( this CliSettings settings ) {
    return settings.ActiveEnvironment == null
      ? null
      : settings.Environments.Find( e => e.Name == settings.ActiveEnvironment );
  }
}