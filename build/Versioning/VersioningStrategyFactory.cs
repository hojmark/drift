using System;
using Drift.Build.Utilities.Versioning;
using Drift.Build.Utilities.Versioning.Abstractions;
using Drift.Build.Utilities.Versioning.Strategies;
using Nuke.Common;
using Nuke.Common.Git;
using Octokit;
using Serilog;

namespace Versioning;

internal sealed class VersioningStrategyFactory( NukeBuild build ) {
  internal IVersioningStrategy Create(
    Configuration configuration,
    string customVersion,
    // Maybe wrap below two in custom type
    IGitHubClient gitHubClient,
    GitRepository repository
  ) {
    if ( build.ExecutionPlan.Contains( build.Release ) && build.ExecutionPlan.Contains( build.PreRelease ) ) {
      throw new InvalidOperationException(
        $"Execution plan cannot contain both {nameof(build.Release)} and {nameof(build.PreRelease)}"
      );
    }

    IVersioningStrategy strategy = new DefaultVersioning( build );

    if ( build.ExecutionPlan.Contains( build.Release ) ) {
      strategy = new ReleaseVersioning( build, configuration, customVersion, repository, gitHubClient );
    }
    else if ( build.ExecutionPlan.Contains( build.PreRelease ) ) {
      strategy = new PreReleaseVersioning( build, configuration, customVersion, repository, gitHubClient );
    }

    Log.Information( "Versioning strategy is {Strategy}", strategy.GetType().Name );

    return strategy;
  }
}