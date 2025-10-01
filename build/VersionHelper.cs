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

internal static class VersionHelper {
  internal static async Task<SemVersion> GetNextReleaseVersion( NukeBuild build, GitRepository repository ) {
    if ( build.CustomVersion != null ) {
      throw new InvalidOperationException( "Cannot specify a custom version when releasing" );
    }

    // .Latest() does not return prereleases
    var releases = await NukeBuild.GitHubClient.Repository.Release.GetAll(
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

    var nextReleaseVersion = GetNextReleaseVersionFromTagNameOrThrow( latestTagName );
    var nextReleaseName = CreateReleaseName( nextReleaseVersion );
    var nextTagName = $"v{nextReleaseVersion}";

    Log.Debug( "Next release is {Name} with tag {TagName}", nextReleaseName, nextTagName );

    Log.Information( "Release version is {Version}", nextReleaseVersion );

    return nextReleaseVersion;
  }

  internal static SemVersion GetDefaultVersion() {
    var version = Nuke.Common.NukeBuild.IsLocalBuild
      ? SemVersion.Parse( "0.0.0-local" )
      : SemVersion.Parse( "0.0.0-ci" );

    Log.Information( "Default version is {Version}", version );

    return version;
  }

  internal static SemVersion GetSpecialReleaseVersion( string customVersion = null ) {
    if ( customVersion == null ) {
      throw new InvalidOperationException(
        $"Must specify {nameof(NukeBuild.CustomVersion)} when releasing a special version" );
    }

    var ver = SemVersion.Parse( customVersion );

    if ( !( ver.Major == 0 && ver.Minor == 0 && ver.Patch == 0 ) ) {
      throw new InvalidOperationException(
        $"{nameof(NukeBuild.CustomVersion)} must start with 0.0.0 when releasing a special version" );
    }

    if ( !ver.IsPrerelease ) {
      throw new InvalidOperationException(
        $"{nameof(NukeBuild.CustomVersion)} must be a prerelease when releasing a special version" );
    }

    if ( ver.PrereleaseIdentifiers.Contains( new PrereleaseIdentifier( "alpha" ) ) ) {
      throw new InvalidOperationException(
        $"{nameof(NukeBuild.CustomVersion)} must not contain 'alpha' when releasing a special version" );
    }

    var metadata = ver.MetadataIdentifiers.ToList();
    var specialIdentifier = new MetadataIdentifier( "special" );
    if ( !metadata.Contains( specialIdentifier ) ) {
      metadata.Add( specialIdentifier );
    }

    var releaseVersion = new SemVersion(
      new BigInteger( 0 ),
      new BigInteger( 0 ),
      new BigInteger( 0 ),
      ver.PrereleaseIdentifiers,
      ver.MetadataIdentifiers
    );

    Log.Information( "Special release version is {Version}", releaseVersion );

    return releaseVersion;
  }


  [CanBeNull]
  static SemVersion GetNextReleaseVersionFromTagNameOrThrow( string latestTagName ) {
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

  internal static string CreateReleaseName( SemVersion version ) {
    return $"Drift CLI {version.WithoutMetadata()}";
  }
}