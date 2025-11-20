using Drift.Build.Utilities.Versioning.Abstractions;
using HLabs.Containers;
using JetBrains.Annotations;
using Nuke.Common;
using Nuke.Common.Git;
using Nuke.Common.Tools.GitHub;
using Octokit;
using Semver;

namespace Drift.Build.Utilities.Versioning;

public abstract class ReleaseVersioningBase : IVersioningStrategy, IReleaseInfo {
  private readonly GitRepository _repository;
  private readonly IGitHubClient _gitHubClient;
  private string? _cachedGitTag;

  protected ReleaseVersioningBase(
    INukeRelease build,
    Configuration configuration,
    GitRepository repository,
    IGitHubClient gitHubClient
  ) {
    _repository = repository;
    _gitHubClient = gitHubClient;

    if ( build.ExecutionPlan.Contains( build.Release ) && build.ExecutionPlan.Contains( build.PreRelease ) ) {
      throw new InvalidOperationException(
        $"Execution plan cannot contain both {nameof(build.Release)} and {nameof(build.PreRelease)}"
      );
    }

    if ( NukeBuild.IsLocalBuild && !build.AllowLocalRelease ) {
      throw new InvalidOperationException( "A local release build was prevented" );
    }

    if ( configuration != Configuration.Release ) {
      throw new InvalidOperationException(
        $"Releases must be built with {nameof(Configuration)}.{nameof(Configuration.Release)}"
      );
    }
  }

  [CanBeNull] public IReleaseInfo Release => this;

  public abstract Task<SemVersion> GetVersionAsync();

  public abstract Task<string> GetNameAsync();

  public async Task<string> GetGitTagAsync() {
    _cachedGitTag ??= await GetReleaseGitTagInternalAsync();
    return _cachedGitTag;
  }

  public virtual async Task<ICollection<ImageReference>> GetImageReferences() {
    return [
      ImageReference.DockerIo( "hojmark", "drift", await GetVersionAsync() )
    ];
  }

  protected static string CreateReleaseName( SemVersion version, bool includeMetadata ) {
    var v = includeMetadata ? version : version.WithoutMetadata();
    return $"Drift CLI {v}";
  }

  private async Task ValidateAvailableOrThrowAsync( string tag ) {
    var existingTags = await _gitHubClient.Repository.GetAllTags(
      _repository.GetGitHubOwner(),
      _repository.GetGitHubName()
    );

    if ( existingTags.Any( t => t.Name == tag ) ) {
      throw new InvalidOperationException( $"Release {tag} already exists" );
    }
  }

  private async Task<string> GetReleaseGitTagInternalAsync() {
    var version = await GetVersionAsync();
    var tag = "v" + version.WithoutMetadata();
    await ValidateAvailableOrThrowAsync( tag );
    return tag;
  }
}