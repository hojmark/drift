using Drift.Cli.Abstractions;
using Drift.Common.IO;

namespace Drift.Cli.Settings.Serialization;

public sealed class DefaultDriftSettingsLocation : IDriftSettingsLocation {
  public string Directory {
    get {
      var configDirOverride = Environment.GetEnvironmentVariable( nameof(EnvVar.Drift_ConfigDir) );

      if ( !string.IsNullOrEmpty( configDirOverride ) ) {
        return configDirOverride;
      }

      return DriftDirectories.ConfigDirectory;
    }
  }
}