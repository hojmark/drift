using HLabs.ImageReferences;
using Semver;

namespace Drift.Build.Utilities.Versioning.Abstractions;

public interface IVersioningStrategy {
  IReleaseInfo? Release {
    get;
  }

  Task<SemVersion> GetVersionAsync();
}

public interface IReleaseInfo {
  Task<string> GetNameAsync();

  Task<string> GetGitTagAsync();

  Task<ICollection<QualifiedImageRef>> GetImageReferences();
}