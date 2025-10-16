using Drift.Build.Utilities;
using Nuke.Common;
using Nuke.Common.Tools.DotNet;
using Versioning;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

// ReSharper disable VariableHidesOuterVariable
// ReSharper disable AllUnderscoreLocalParameterName
// ReSharper disable UnusedMember.Local

internal partial class NukeBuild {
  Target Restore => _ => _
    .DependsOn( CleanProjects )
    .Executes( () => {
        using var _ = new OperationTimer( nameof(Restore) );

        DotNetRestore( s => s
          .SetProjectFile( Solution )
        );
      }
    );

  Target Build => _ => _
    .DependsOn( Restore )
    .Executes( async () => {
        using var _ = new OperationTimer( nameof(Build) );

        var version = await Versioning.Value.GetVersionAsync();

        DotNetBuild( s => s
          .SetProjectFile( Solution )
          .SetConfiguration( Configuration )
          .SetVersionProperties( version )
          .SetBinaryLog( BinaryBuildLogName )
          .EnableNoLogo()
          .EnableNoRestore()
        );
      }
    );
}