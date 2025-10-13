using System.Linq;
using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Utilities.Collections;
using Serilog;
using Utilities;

// ReSharper disable VariableHidesOuterVariable
// ReSharper disable AllUnderscoreLocalParameterName
// ReSharper disable UnusedMember.Local

sealed partial class NukeBuild {
  Target Clean => _ => _
    .DependsOn( CleanProjects, CleanArtifacts );

  Target CleanProjects => _ => _
    .Before( Restore )
    .Executes( () => {
        using var _ = new OperationTimer( nameof(CleanProjects) );

        var dirsToDelete = Solution.AllProjects
          .Where( project => !IsBuildProject( project ) )
          .SelectMany( project => new[] {
              // Build directories
              project.Directory / "bin", project.Directory / "obj",

              // trx logs
              project.Directory / "TestResults"
            }
          )
          .Where( dir => dir.Exists() )
          .ToList();

        if ( dirsToDelete.IsEmpty() ) {
          Log.Debug( "No projects to clean" );
        }

        dirsToDelete.ForEach( d => {
          Log.Debug( "Deleting {BinOrObjDir}", d.ToString() );
          d.DeleteDirectory();
        } );
      }
    );

  Target CleanArtifacts => _ => _
    .Before( Restore )
    .Executes( () => {
        using var _ = new OperationTimer( nameof(CleanArtifacts) );

        Log.Debug( "Cleaning {Directory}", Paths.PublishDirectory );
        Paths.PublishDirectory.CreateOrCleanDirectory();

        Log.Debug( "Cleaning {Directory}", Paths.ArtifactsDirectory );
        Paths.ArtifactsDirectory.CreateOrCleanDirectory();
      }
    );

  private static bool IsBuildProject( Project project ) => project.Path == BuildProjectFile;
}