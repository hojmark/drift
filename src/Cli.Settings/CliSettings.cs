using Drift.Cli.Settings.FeatureFlags;

namespace Drift.Cli.Settings;

public partial class CliSettings {
  public List<FeatureFlagSetting> Features {
    get;
    set;
  } = [];

  public Theme.Theme Theme {
    get;
    set;
  } = Settings.Theme.Theme.Default;
}