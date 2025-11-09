namespace Drift.Cli.Settings.V1_preview.FeatureFlags;

public static class CliSettingsExtensions {
  public static bool IsFeatureEnabled( this CliSettings settings, FeatureFlag flag ) {
    var entry = settings.Features.Find( f => f.Name == flag );
    return entry?.Enabled ?? false;
  }
}