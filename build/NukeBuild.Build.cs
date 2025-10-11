using Nuke.Common;
using Nuke.Common.Tools.DotNet;
using Utilities;
using Versioning;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

// ReSharper disable VariableHidesOuterVariable
// ReSharper disable AllUnderscoreLocalParameterName
// ReSharper disable UnusedMember.Local

internal partial class NukeBuild {
  Target Restore => _ => _
    .DependsOn( CleanProjects )
    .Executes( () => {
        using var _ = new TargetLifecycle( nameof(Restore) );

        DotNetRestore( s => s
          .SetProjectFile( Solution )
        );
      }
    );

  Target Build => _ => _
    .DependsOn( Restore )
    .Executes( () => {
        using var _ = new TargetLifecycle( nameof(Build) );

        DotNetBuild( s => s
          .SetProjectFile( Solution )
          .SetConfiguration( Configuration )
          .SetVersionProperties( SemVer )
          .SetBinaryLog( BinaryBuildLogName )
          .EnableNoLogo()
          .EnableNoRestore()
        );
      }
    );
}