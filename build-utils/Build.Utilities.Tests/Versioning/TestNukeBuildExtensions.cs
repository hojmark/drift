using Drift.Build.Utilities.Tests.NukeBuild;
using Nuke.Common;
using Nuke.Common.Execution;

namespace Drift.Build.Utilities.Tests.Versioning;

internal static class TestNukeBuildExtensions {
  extension<T>( T build ) where T : TestNukeBuild {
    internal T WithExecutionPlan( params Func<T, Target>[] targets ) {
      build.ExecutionPlan = targets.Select( target => new ExecutableTarget { Factory = target( build ) } ).ToList();
      return build;
    }

    internal T WithReleaseType( ReleaseType releaseType ) {
      build.ReleaseType = releaseType;
      return build;
    }
  }
}