using Drift.Cli.Abstractions;

namespace Drift.Cli.Settings.Serialization;

public interface ISettingsLocation {
  string Directory {
    get;
  }

  string File {
    get {
      return Path.Combine( Directory, Files.SettingsFileName );
    }
  }
}