namespace Drift.Common.IO;

public sealed class DefaultDriftDataLocation : IDriftDataLocation {
  public string Directory => DriftDirectories.DriftDataDirectory;
}