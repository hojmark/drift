using Drift.Cli.Settings.Serialization;

namespace Drift.Cli.Settings.Tests;

internal sealed class TemporarySettingsLocationProvider : ISettingsLocationProvider {
  private readonly string _directory = Path.Combine( Path.GetTempPath(), Guid.NewGuid().ToString() );

  public string GetDirectory() => _directory;
}