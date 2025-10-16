using Drift.Build.Utilities.Versioning.Abstractions;
using Nuke.Common;
using Semver;

namespace Drift.Build.Utilities.Versioning.Strategies;

public sealed class DefaultVersioning( INukeRelease build ) : IVersioningStrategy {
  private readonly SemVersion _version = build.IsLocalBuild
    ? SemVersion.Parse( "0.0.0-local" )
    : SemVersion.Parse( "0.0.0-ci" );

  public IReleaseInfo? Release {
    get;
  } = null;

  public Task<SemVersion> GetVersionAsync() {
    return Task.FromResult( _version );
  }

  public bool SupportsTarget( Target target ) {
    return !build.ExecutionPlan.Contains( build.Release ) &&
           !build.ExecutionPlan.Contains( build.PreRelease ) &&
           target != build.Release &&
           target != build.PreRelease;
  }
}