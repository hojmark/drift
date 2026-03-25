using Drift.Build.Utilities.Versioning.Abstractions;
using Drift.Build.Utilities.Versioning.Strategies;
using Nuke.Common;
using Nuke.Common.Git;
using Octokit;
using Serilog;

namespace Drift.Build.Utilities.Versioning;

public sealed class VersioningStrategyFactory( INukeRelease build, TimeProvider? timeProvider = null ) {
  public IVersioningStrategy Create(
    Configuration configuration,
    string? prereleaseIdentifiers,
    string? exactVersion,
    // Maybe wrap below two in custom type
    IGitHubClient gitHubClient,
    GitRepository repository
  ) {
    var releaseType = build.ReleaseType;

    ValidateExecutionPlanOrThrow( releaseType );

    if ( !string.IsNullOrWhiteSpace( exactVersion ) ) {
      if ( releaseType == ReleaseType.None ) {
        throw new InvalidOperationException(
          $"{nameof(exactVersion)} cannot be used with {nameof(ReleaseType)}.{nameof(ReleaseType.None)}"
        );
      }

      var strategy = new ExactVersioning( configuration, exactVersion, repository, gitHubClient );
      Log.Information( "Versioning strategy is {Strategy}", strategy.GetType().Name );
      return strategy;
    }

    if ( !string.IsNullOrWhiteSpace( prereleaseIdentifiers ) && releaseType != ReleaseType.PreRelease ) {
      throw new InvalidOperationException(
        $"{nameof(prereleaseIdentifiers)} can only be used with {nameof(ReleaseType)}.{nameof(ReleaseType.PreRelease)} " +
        $"but got {releaseType}"
      );
    }

    IVersioningStrategy baseStrategy = releaseType switch {
      ReleaseType.Release => new ReleaseVersioning( configuration, repository, gitHubClient ),
      ReleaseType.PreRelease => new PreReleaseVersioning(
        configuration,
        prereleaseIdentifiers,
        repository,
        gitHubClient,
        timeProvider ?? TimeProvider.System
      ),
      ReleaseType.None => new DefaultVersioning( build ),
      _ => throw new InvalidOperationException( $"Unknown {nameof(ReleaseType)}: {releaseType}" ),
    };

    Log.Information( "Versioning strategy is {Strategy}", baseStrategy.GetType().Name );

    return baseStrategy;
  }

  private void ValidateExecutionPlanOrThrow( ReleaseType releaseType ) {
    var planHasCreateRelease = build.ExecutionPlan.Contains( build.CreateRelease );
    var planHasCreatePreRelease = build.ExecutionPlan.Contains( build.CreatePreRelease );

    if ( planHasCreateRelease && planHasCreatePreRelease ) {
      throw new InvalidOperationException(
        $"Execution plan cannot contain both {nameof(build.CreateRelease)} and {nameof(build.CreatePreRelease)}"
      );
    }

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
  }
}