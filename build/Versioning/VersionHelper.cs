using System;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Nuke.Common.Git;
using Nuke.Common.Tools.GitHub;
using Semver;
using Serilog;

namespace Versioning;

internal static class VersionHelper {
  internal static async Task<SemVersion> GetNextReleaseVersion( NukeBuild build, GitRepository repository ) {
    if ( build.CustomVersion != null ) {
      throw new InvalidOperationException( "Cannot specify a custom version when releasing" );
    }

    // .Latest() does not return prereleases
    var releases = await build.GitHubClient.Repository.Release.GetAll(
      repository.GetGitHubOwner(),
      repository.GetGitHubName()
    );
    var latest = releases
      .OrderByDescending( r => r.PublishedAt )
      .FirstOrDefault( r => !r.Draft );
    var latestTagName = latest?.TagName;

    if ( latest == null ) {
      // TODO update when having first version
      //Log.Error( "No releases found. Cannot determine next version." );
      //return;
      latestTagName = "v1.0.0-alpha.0";
    }

    Log.Debug( "Latest release is {Name} with tag {TagName}", latest?.Name, latestTagName );

    var version = GetNextReleaseVersionFromTagNameOrThrow( latestTagName );
    var releaseName = CreateReleaseName( version, includeMetadata: false );
    var tagName = $"v{version}";

    Log.Debug( "Next release is {Name} with tag {TagName}", releaseName, tagName );

    Log.Information( "Release version is {Version}", version );

    return version;
  }

  internal static SemVersion GetDefaultVersion() {
    var version = Nuke.Common.NukeBuild.IsLocalBuild
      ? SemVersion.Parse( "0.0.0-local" )
      : SemVersion.Parse( "0.0.0-ci" );

    Log.Information( "Default version is {Version}", version );

    return version;
  }

  internal static SemVersion GetPreReleaseVersion( string customVersion = null ) {
    if ( customVersion == null ) {
      throw new InvalidOperationException(
        $"Must specify {nameof(NukeBuild.CustomVersion)} when releasing a pre-release version"
      );
    }

    var ver = SemVersion.Parse( customVersion );

    if ( !( ver.Major == 0 && ver.Minor == 0 && ver.Patch == 0 ) ) {
      throw new InvalidOperationException(
        $"{nameof(NukeBuild.CustomVersion)} must start with 0.0.0 when releasing a pre-release version" );
    }

    if ( !ver.IsPrerelease ) {
      throw new InvalidOperationException(
        $"{nameof(NukeBuild.CustomVersion)} must be a prerelease when releasing a pre-release version" );
    }

    if ( ver.PrereleaseIdentifiers.Contains( new PrereleaseIdentifier( "alpha" ) ) ) {
      throw new InvalidOperationException(
        $"{nameof(NukeBuild.CustomVersion)} must not contain 'alpha' when releasing a pre-release version" );
    }

    var updatedPrereleaseIdentifiers = ver.PrereleaseIdentifiers.ToList();
    var date = new PrereleaseIdentifier( DateTime.UtcNow.ToString( "yyyyMMddHHmmss" ) );
    updatedPrereleaseIdentifiers.Add( date );

    var updatedMetadata = ver.MetadataIdentifiers.ToList();
    var preReleaseIdentifier = new MetadataIdentifier( "prerelease" );
    if ( !updatedMetadata.Contains( preReleaseIdentifier ) ) {
      updatedMetadata.Add( preReleaseIdentifier );
    }

    var version = new SemVersion(
      new BigInteger( 0 ),
      new BigInteger( 0 ),
      new BigInteger( 0 ),
      updatedPrereleaseIdentifiers,
      updatedMetadata
    );
    var releaseName = CreateReleaseName( version, includeMetadata: true );
    var tagName = CreateTagName( version );

    Log.Debug( "Pre-release is {Name} with tag {TagName}", releaseName, tagName );

    Log.Information( "Pre-release version is {Version}", version );

    return version;
  }

  internal static string CreateReleaseName( SemVersion version, bool includeMetadata ) {
    var v = includeMetadata ? version : version.WithoutMetadata();
    return $"Drift CLI {v}";
  }

  internal static string CreateTagName( SemVersion version ) => "v" + version.WithoutMetadata();

  [CanBeNull]
  private static SemVersion GetNextReleaseVersionFromTagNameOrThrow( string latestTagName ) {
    var latestVersion = SemVersion.Parse(
      // Skip 'v'
      latestTagName[1..]
    );

    if ( latestVersion.PrereleaseIdentifiers.Count != 2 ||
         latestVersion.PrereleaseIdentifiers[0] != "alpha" ||
         !int.TryParse(
           latestVersion.PrereleaseIdentifiers[1],
           CultureInfo.InvariantCulture,
           out int latestAlphaNumber
         )
       ) {
      throw new InvalidOperationException( "Cannot determine next version. Latest release has tag " + latestTagName );
    }

    var nextAlphaNumber = ++latestAlphaNumber;

    return latestVersion.WithPrerelease( "alpha", nextAlphaNumber.ToString( CultureInfo.InvariantCulture ) );
  }
}