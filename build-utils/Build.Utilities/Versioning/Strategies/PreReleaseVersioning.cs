using Nuke.Common.Git;
using Octokit;
using Semver;

namespace Drift.Build.Utilities.Versioning.Strategies;

public sealed class PreReleaseVersioning(
  Configuration configuration,
  string? prereleaseIdentifiers,
  GitRepository repository,
  IGitHubClient gitHubClient,
  TimeProvider timeProvider
) : ReleaseVersioningBase( configuration, repository, gitHubClient ) {
  private string? _timestamp; // Cache to support multiple calls to GetVersionAsync()

  public override Task<SemVersion> GetVersionAsync() {
    if ( string.IsNullOrWhiteSpace( prereleaseIdentifiers ) ) {
      throw new InvalidOperationException(
        "Must specify pre-release identifiers when releasing a pre-release version"
      );
    }

    if ( SemVersion.TryParse( prereleaseIdentifiers, out _ ) ) {
      throw new InvalidOperationException(
        $"Pre-release identifiers must not be a full version string (e.g. use 'attempt.42' not '0.0.0-attempt.42'). Got: '{prereleaseIdentifiers}'"
      );
    }

    ValidateIdentifiers( prereleaseIdentifiers );

    var parsed = SemVersion.ParsedFrom( 0, 0, 0, prereleaseIdentifiers );

    var identifiers = parsed.PrereleaseIdentifiers.ToList();
    _timestamp ??= timeProvider.GetUtcNow().ToString( "yyyyMMddHHmmss" );
    identifiers.Add( new PrereleaseIdentifier( _timestamp ) );

    return Task.FromResult( new SemVersion(
      parsed.Major,
      parsed.Minor,
      parsed.Patch,
      identifiers,
      parsed.MetadataIdentifiers
    ) );
  }

  public override async Task<string> GetNameAsync() {
    return CreateReleaseName( await GetVersionAsync(), includeMetadata: true );
  }

  private static void ValidateIdentifiers( string identifiers ) {
    if ( identifiers.Split( '.' ).Contains( "alpha" ) ) {
      throw new InvalidOperationException( "Pre-release identifiers must not contain 'alpha'" );
    }
  }
}