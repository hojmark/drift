namespace Drift.Cli.Settings.Schema;

public static class SettingsSchemaProvider {
  internal static string AsText( SettingsVersion version ) {
    return EmbeddedResourceProvider.GetStream( GetPath( version ) ).ReadText();
  }

  private static string GetPath( SettingsVersion version ) {
    return $"schemas/{version.ToJsonSchemaFileName()}";
  }
}