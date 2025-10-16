using System.Globalization;
using Drift.Build.Utilities.ContainerImage;
using Drift.Build.Utilities.Versioning.Abstractions;
using JetBrains.Annotations;
using Nuke.Common.Git;
using Nuke.Common.Tools.GitHub;
using Octokit;
using Semver;
using Serilog;

namespace Drift.Build.Utilities.Versioning.Strategies;

public sealed class ReleaseVersioning(
  INukeRelease build,
  Configuration configuration,
  string? customVersion,
  GitRepository repository,
  IGitHubClient gitHubClient
) : ReleaseVersioningBase( build, configuration, repository, gitHubClient ) {
  private SemVersion? _cachedVersion;

  public override async Task<SemVersion> GetVersionAsync() {
    _cachedVersion ??= await GetVersionInternalAsync();
    return _cachedVersion;
  }

  private async Task<SemVersion> GetVersionInternalAsync() {
    if ( customVersion != null ) {
      throw new InvalidOperationException( "Cannot specify a custom version when releasing" );
    }

    // TODO can now use .Latest(), since main release is no longer a prerelease
    // .Latest() does not return prereleases
    var releases = await gitHubClient.Repository.Release.GetAll(
      repository.GetGitHubOwner(),
      repository.GetGitHubName()
    );
    var latest = releases
      .OrderByDescending( r => r.PublishedAt )
      .FirstOrDefault( r => !r.Draft );
    var latestTagName = latest?.TagName;

    if ( latest == null ) {
      throw new InvalidOperationException( "No releases found. Cannot determine next version." );
    }

    Log.Debug( "Latest release is {Name} with git tag {TagName}", latest.Name, latestTagName );

    return GetNextReleaseVersionFromTagNameOrThrow( latestTagName );
  }

  public override async Task<string> GetNameAsync() {
    return CreateReleaseName( await GetVersionAsync(), includeMetadata: false );
  }

  public override async Task<ICollection<ImageReference>> GetImageReferences() {
    return [
      ImageReference.DockerIo( "hojmark", "drift", LatestVersion.Instance ),
      ..await base.GetImageReferences()
    ];
  }

  [CanBeNull]
  private static SemVersion GetNextReleaseVersionFromTagNameOrThrow( string latestTag ) {
    var latestVersion = SemVersion.Parse(
      // Skip 'v'
      latestTag[1..]
    );

    if ( latestVersion.PrereleaseIdentifiers.Count != 2 ||
         latestVersion.PrereleaseIdentifiers[0] != "alpha" ||
         !int.TryParse(
           latestVersion.PrereleaseIdentifiers[1],
           CultureInfo.InvariantCulture,
           out int latestAlphaNumber
         )
       ) {
      throw new InvalidOperationException( "Cannot determine next version. Latest release has tag " + latestTag );
    }

    var nextAlphaNumber = ++latestAlphaNumber;

    return latestVersion.WithPrerelease( "alpha", nextAlphaNumber.ToString( CultureInfo.InvariantCulture ) );
  }
}