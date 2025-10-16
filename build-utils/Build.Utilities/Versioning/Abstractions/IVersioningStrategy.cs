using Drift.Build.Utilities.ContainerImage;
using Nuke.Common;
using Semver;

namespace Drift.Build.Utilities.Versioning.Abstractions;

// TODO or is it build type? in that case, Default should probably be Other
public interface IVersioningStrategy {
  IReleaseInfo? Release {
    get;
  }

  Task<SemVersion> GetVersionAsync();

  bool SupportsTarget( Target target );
}

public interface IReleaseInfo {
  Task<string> GetReleaseNameAsync();

  Task<string> GetReleaseGitTagAsync();

  Task<ICollection<ImageReference>> GetContainerImageReference();
}