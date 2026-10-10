namespace Drift.Common.IO;

/// <summary>
/// Resolves Drift's root data location.
/// </summary>
/// <seealso cref="IDriftSettingsLocation"/>
public interface IDriftDataLocation {
  string Directory {
    get;
  }
}