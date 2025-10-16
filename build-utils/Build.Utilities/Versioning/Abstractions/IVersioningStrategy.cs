using Drift.Build.Utilities.ContainerImage;
using Nuke.Common;
using Semver;

namespace Drift.Build.Utilities.Versioning.Abstractions;

public interface IVersioningStrategy {
  IReleaseInfo? Release {
    get;
  }

  Task<SemVersion> GetVersionAsync();
}

public interface IReleaseInfo {
  Task<string> GetReleaseNameAsync();

  Task<string> GetReleaseGitTagAsync();

  Task<ICollection<ImageReference>> GetContainerImageReference();
}