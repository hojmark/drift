using Drift.Build.Utilities.Versioning.Abstractions;
using Drift.Build.Utilities.Versioning.Strategies;
using Nuke.Common;
using Nuke.Common.Git;
using Octokit;
using Serilog;

namespace Drift.Build.Utilities.Versioning;

public sealed class VersioningStrategyFactory( INukeRelease build ) {
  public IVersioningStrategy Create(
    Configuration configuration,
    string? prereleaseIdentifiers,
    string? exactVersion,
    // Maybe wrap below two in custom type
    IGitHubClient? gitHubClient,
    GitRepository? repository
  ) {
    var releaseType = build.ReleaseType;
    var planHasCreateRelease = build.ExecutionPlan.Contains( build.CreateRelease );
    var planHasCreatePreRelease = build.ExecutionPlan.Contains( build.CreatePreRelease );

    if ( planHasCreateRelease && planHasCreatePreRelease ) {
      throw new InvalidOperationException(
        $"Execution plan cannot contain both {nameof(build.CreateRelease)} and {nameof(build.CreatePreRelease)}"
      );
    }

    // When a release target is in the plan, ReleaseType must match exactly.
    if ( planHasCreateRelease && releaseType != ReleaseType.Release ) {
      throw new InvalidOperationException(
        $"{nameof(build.CreateRelease)} requires {nameof(build.ReleaseType)} to be {nameof(ReleaseType.Release)} " +
        $"but got {releaseType}"
      );
    }

    if ( planHasCreatePreRelease && releaseType != ReleaseType.PreRelease ) {
      throw new InvalidOperationException(
        $"{nameof(build.CreatePreRelease)} requires {nameof(build.ReleaseType)} to be {nameof(ReleaseType.PreRelease)} " +
        $"but got {releaseType}"
      );
    }

    // exactVersion carries a pre-computed version from the version job, valid for both PreRelease
    // and Release builds. Not valid for None — that implies a local/CI default build where no
    // release is taking place.
    if ( !string.IsNullOrWhiteSpace( exactVersion ) ) {
      if ( releaseType == ReleaseType.None ) {
        throw new InvalidOperationException(
          $"{nameof(exactVersion)} cannot be used with {nameof(ReleaseType)}.{nameof(ReleaseType.None)}"
        );
      }

      var strategy = new ExactVersioning( configuration, exactVersion, repository!, gitHubClient! );
      Log.Information( "Versioning strategy is {Strategy}", strategy.GetType().Name );
      return strategy;
    }

    // prereleaseIdentifiers is only meaningful for PreRelease builds.
    if ( !string.IsNullOrWhiteSpace( prereleaseIdentifiers ) && releaseType != ReleaseType.PreRelease ) {
      throw new InvalidOperationException(
        $"{nameof(prereleaseIdentifiers)} can only be used with {nameof(ReleaseType)}.{nameof(ReleaseType.PreRelease)} " +
        $"but got {releaseType}"
      );
    }

    // When ReleaseType is set but neither release target is in the plan (build jobs), that is
    // intentional — the strategy is selected so artifact names are versioned correctly.
    // When ReleaseType is None and a release target somehow ended up in the plan, the guards
    // above already caught that.

    IVersioningStrategy baseStrategy = releaseType switch {
      ReleaseType.Release => new ReleaseVersioning( configuration, repository!, gitHubClient! ),
      ReleaseType.PreRelease => new PreReleaseVersioning(
        configuration,
        prereleaseIdentifiers,
        repository!,
        gitHubClient!
      ),
      ReleaseType.None => new DefaultVersioning( build ),
      _ => throw new InvalidOperationException( $"Unknown {nameof(ReleaseType)}: {releaseType}" ),
    };

    Log.Information( "Versioning strategy is {Strategy}", baseStrategy.GetType().Name );

    return baseStrategy;
  }
}