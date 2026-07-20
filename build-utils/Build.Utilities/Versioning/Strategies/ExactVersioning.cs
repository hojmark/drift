using HLabs.ImageReferences;
using Nuke.Common.Git;
using Octokit;
using Semver;

namespace Drift.Build.Utilities.Versioning.Strategies;

public sealed class ExactVersioning(
  Configuration configuration,
  string? version,
  ReleaseType releaseType,
  GitRepository repository,
  IGitHubClient gitHubClient
) : ReleaseVersioningBase( configuration, repository, gitHubClient ) {
  public override Task<SemVersion> GetVersionAsync() {
    if ( string.IsNullOrWhiteSpace( version ) ) {
      throw new InvalidOperationException( "Must specify version when using exact versioning" );
    }

    return Task.FromResult( SemVersion.Parse( version ) );
  }

  public override async Task<string> GetNameAsync() {
    return CreateReleaseName( await GetVersionAsync(), includeMetadata: true );
  }

  public override async Task<ICollection<QualifiedImageRef>> GetImageReferences() {
    var refs = await base.GetImageReferences();
    if ( releaseType == ReleaseType.Release ) { // TODO not very clean location
      return ["hojmark/drift".Image().Qualify( Tag.Latest ), ..refs];
    }

    return refs;
  }
}