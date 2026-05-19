using Drift.Cli.Settings.Serialization;

namespace Drift.Cli.Settings.Tests;

// Currently used by FeatureFlagTests in Cli.Tests
#pragma warning disable CA1515
public sealed class TemporarySettingsLocationProvider : ISettingsLocationProvider {
#pragma warning restore CA1515
  private readonly string _directory = Path.Combine( Path.GetTempPath(), Guid.NewGuid().ToString() );

  public string GetDirectory() => _directory;
}