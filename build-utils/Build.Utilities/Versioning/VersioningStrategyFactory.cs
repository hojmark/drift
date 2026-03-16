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
    string? customVersion,
    // Maybe wrap below two in custom type
    IGitHubClient gitHubClient,
    GitRepository repository
  ) {
    if ( build.ExecutionPlan.Contains( build.CreateRelease ) && build.ExecutionPlan.Contains( build.CreatePreRelease ) ) {
      throw new InvalidOperationException(
        $"Execution plan cannot contain both {nameof(build.CreateRelease)} and {nameof(build.CreatePreRelease)}"
      );
    }

    IVersioningStrategy strategy = new DefaultVersioning( build );

    if ( build.ExecutionPlan.Contains( build.CreateRelease ) ) {
      strategy = new ReleaseVersioning( build, configuration, customVersion, repository, gitHubClient );
    }
    else if ( build.ExecutionPlan.Contains( build.CreatePreRelease ) ) {
      strategy = new PreReleaseVersioning( build, configuration, customVersion, repository, gitHubClient );
    }

    Log.Information( "Versioning strategy is {Strategy}", strategy.GetType().Name );

    return strategy;
  }
}