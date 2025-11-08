using Drift.Cli.Settings.V1_preview.Appearance;
using Drift.Cli.Settings.V1_preview.FeatureFlags;

namespace Drift.Cli.Settings.V1_preview;

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