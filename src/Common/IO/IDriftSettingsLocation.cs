using Drift.Cli.Abstractions;

namespace Drift.Common.IO;

/// <summary>
/// Resolves Drift's settings location.
/// </summary>
public interface IDriftSettingsLocation {
  string Directory {
    get;
  }

  string File {
    get {
      return Path.Combine( Directory, Files.SettingsFileName );
    }
  }
}