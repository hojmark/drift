using System.Numerics;
using Drift.Build.Utilities.Versioning.Abstractions;
using NuGet.Packaging.Signing;
using Nuke.Common.Git;
using Octokit;
using Semver;

namespace Drift.Build.Utilities.Versioning.Strategies;

public sealed class PreReleaseVersioning(
  INukeRelease build,
  Configuration configuration,
  string? customVersion,
  GitRepository repository,
  IGitHubClient gitHubClient
) : ReleaseVersioningBase( build, configuration, repository, gitHubClient ) {
  private string? _timestamp; // Cache to support multiple calls to GetVersionAsync()

  public override Task<SemVersion> GetVersionAsync() {
    if ( string.IsNullOrWhiteSpace( customVersion ) ) {
      throw new InvalidOperationException(
        "Must specify custom version when releasing a pre-release version"
      );
    }

    var ver = SemVersion.Parse( customVersion );

    if ( !( ver.Major == 0 && ver.Minor == 0 && ver.Patch == 0 ) ) {
      throw new InvalidOperationException(
        "Custom version must start with 0.0.0 when releasing a pre-release version" );
    }

    if ( !ver.IsPrerelease ) {
      throw new InvalidOperationException(
        "Custom version must be a prerelease when releasing a pre-release version" );
    }

    if ( ver.PrereleaseIdentifiers.Contains( new PrereleaseIdentifier( "alpha" ) ) ) {
      throw new InvalidOperationException(
        "Custom version must not contain 'alpha' when releasing a pre-release version" );
    }

    var updatedPrereleaseIdentifiers = ver.PrereleaseIdentifiers.ToList();
    _timestamp ??= DateTime.UtcNow.ToString( "yyyyMMddHHmmss" );
    var date = new PrereleaseIdentifier( _timestamp );
    updatedPrereleaseIdentifiers.Add( date );

    var updatedMetadata = ver.MetadataIdentifiers.ToList();
    /*var preReleaseIdentifier = new MetadataIdentifier( "prerelease" );
    if ( !updatedMetadata.Contains( preReleaseIdentifier ) ) {
      updatedMetadata.Add( preReleaseIdentifier );
    }*/

    var version = new SemVersion(
      new BigInteger( 0 ),
      new BigInteger( 0 ),
      new BigInteger( 0 ),
      updatedPrereleaseIdentifiers,
      updatedMetadata
    );

    return Task.FromResult( version );
  }

  public override async Task<string> GetNameAsync() {
    return CreateReleaseName( await GetVersionAsync(), includeMetadata: true );
  }
}