using Drift.Cli.Settings.Appearance;
using Drift.Cli.Settings.FeatureFlags;

namespace Drift.Cli.Settings;

public partial class CliSettings {
  public List<FeatureFlagSetting> Features {
    get;
    set;
  } = [];

  public AppearanceSettings Appearance {
    get;
    set;
  } = new(OutputFormatSetting.Default);
}