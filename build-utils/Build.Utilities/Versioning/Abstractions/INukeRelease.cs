using Nuke.Common;

namespace Drift.Build.Utilities.Versioning.Abstractions;

public interface INukeRelease : INukeBuild {
  public Target Release {
    get;
  }

  public Target PreRelease {
    get;
  }

  bool AllowLocalRelease {
    get;
  }
}