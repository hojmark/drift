namespace Drift.Cli.Settings.Schema;

public static class SettingsVersionExtensions {
  public static string ToJsonSchemaFileName( this SettingsVersion version ) {
    return $"drift-settings-{version.ToJsonSchemaFileNameVersionPart()}.schema.json";
  }

  private static string ToJsonSchemaFileNameVersionPart( this SettingsVersion version ) {
    return version.ToString().ToLowerInvariant().Replace( "_", "-" );
  }
}