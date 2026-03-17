using Nuke.Common;

namespace Drift.Build.Utilities.Versioning.Abstractions;

public interface INukeRelease : INukeBuild {
  public Target CreateRelease {
    get;
  }

  public Target CreatePreRelease {
    get;
  }

  ReleaseType ReleaseType {
    get;
  }
}