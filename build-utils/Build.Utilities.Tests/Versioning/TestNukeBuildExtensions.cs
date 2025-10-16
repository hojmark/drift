using Drift.Build.Utilities.Tests.NukeBuild;
using Nuke.Common;
using Nuke.Common.Execution;

namespace Drift.Build.Utilities.Tests.Versioning;

internal static class TestNukeBuildExtensions {
  internal static T WithExecutionPlan<T>( this T build, params Func<T, Target>[] targets ) where T : TestNukeBuild {
    build.ExecutionPlan = targets.Select( target => new ExecutableTarget { Factory = target( build ) } ).ToList();
    return build;
  }

  internal static T AllowLocalRelease<T>( this T build, bool allowLocalRelease = true ) where T : TestNukeBuild {
    build.AllowLocalRelease = allowLocalRelease;
    return build;
  }
}