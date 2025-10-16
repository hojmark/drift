using Drift.Build.Utilities.Versioning.Abstractions;
using Nuke.Common;
using Nuke.Common.Execution;

namespace Drift.Build.Utilities.Tests.NukeBuild;

internal class TestNukeBuild : Nuke.Common.NukeBuild, INukeRelease {
  internal TestNukeBuild() {
    ExecutionPlan = new List<ExecutableTarget>();
  }

  public bool AllowLocalRelease {
    get;
    set;
  }

  public Target Release => _ => _
    .Executes( () => {
      }
    );

  public Target PreRelease => _ => _
    .Executes( () => {
      }
    );
}