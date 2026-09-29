using Drift.Cli.Settings.Serialization;

namespace Drift.Cli.Settings.Tests;

// Currently used by FeatureFlagTests in Cli.Tests
#pragma warning disable CA1515
public sealed class TemporarySettingsLocation : ISettingsLocation {
#pragma warning restore CA1515

  public string Directory {
    get;
  } = Path.Combine( Path.GetTempPath(), Guid.NewGuid().ToString() );
}