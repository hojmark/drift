using Drift.Cli.Abstractions;
using Drift.Common.IO;

namespace Drift.Cli.Settings.Serialization;

public sealed class DefaultSettingsLocation : ISettingsLocation {
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