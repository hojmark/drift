using Drift.Build.Utilities.Versioning.Abstractions;
using Nuke.Common;
using Nuke.Common.Execution;

namespace Drift.Build.Utilities.Tests.NukeBuild;

internal class TestNukeBuild : Nuke.Common.NukeBuild, INukeRelease {
  internal TestNukeBuild() {
    ExecutionPlan = new List<ExecutableTarget>();
  }

  public ReleaseType ReleaseType {
    get;
    set;
  } = ReleaseType.None;

  public Target CreateRelease => _ => _
    .Executes( () => {
      }
    );

  public Target CreatePreRelease => _ => _
    .Executes( () => {
      }
    );
}