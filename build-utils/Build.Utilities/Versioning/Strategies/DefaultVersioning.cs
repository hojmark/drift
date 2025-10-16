using Drift.Build.Utilities.Versioning.Abstractions;
using Nuke.Common;
using Semver;

namespace Drift.Build.Utilities.Versioning.Strategies;

public sealed class DefaultVersioning( INukeRelease build ) : IVersioningStrategy {
  private readonly SemVersion _version = build.IsLocalBuild
    ? SemVersion.Parse( "0.0.0-local" )
    : SemVersion.Parse( "0.0.0-ci" );

  public IReleaseInfo? Release => null;

  public Task<SemVersion> GetVersionAsync() {
    return Task.FromResult( _version );
  }
}