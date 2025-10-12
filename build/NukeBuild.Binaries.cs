using System.IO;
using System.Linq;
using Nuke.Common.IO;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Serilog;
using Utilities;
using Versioning;
using static Nuke.Common.Tools.DotNet.DotNetTasks;
using Target = Nuke.Common.Target;

// ReSharper disable VariableHidesOuterVariable
// ReSharper disable AllUnderscoreLocalParameterName
// ReSharper disable UnusedMember.Local

sealed partial class NukeBuild {
  private static readonly string[] FilesToDistribute = ["drift", "drift.dbg"];

  Target PublishBinaries => _ => _
    .DependsOn( Build, CleanArtifacts )
    .Executes( () => {
        using var _ = new OperationTimer( nameof(PublishBinaries) );

        // TODO https://nuke.build/docs/common/cli-tools/#combinatorial-modifications
        foreach ( var runtime in SupportedRuntimes ) {
          var publishDir = Paths.PublishDirectoryForRuntime( runtime );

          Log.Information( "Publishing {Runtime} build to {PublishDir}", runtime, publishDir );
          DotNetPublish( s => s
            .SetProject( Solution.Cli )
            .SetConfiguration( Configuration )
            .SetOutput( publishDir )
            .SetSelfContained( true )
            .SetVersionProperties( SemVer )
            // TODO if not specifying a RID, apparently only x64 gets built on x64 host
            .SetRuntime( runtime )
            .SetProcessAdditionalArguments( $"-bl:{BinaryPublishLogName}" )
            .EnableNoLogo()
            .EnableNoRestore()
            .EnableNoBuild()
          );
        }
      }
    );

  Target PackBinaries => _ => _
    .DependsOn( PublishBinaries, CleanArtifacts )
    .Executes( () => {
        using var _ = new OperationTimer( nameof(PackBinaries) );

        foreach ( var runtime in SupportedRuntimes ) {
          var publishDir = Paths.PublishDirectoryForRuntime( runtime );
          var artifactFile = Paths.ArtifactsDirectory / $"drift_{SemVer.WithoutMetadata()}_{runtime}.tar.gz";

          Log.Information( "Creating {ArtifactFile}", artifactFile );
          var files = publishDir
            .GetFiles()
            .Where( file => FilesToDistribute.Contains( file.Name ) )
            .ToList();
          Log.Debug( "Including files: {Files}", string.Join( ", ", files.Select( f => f.Name ) ) );
          publishDir.TarGZipTo( artifactFile, files, fileMode: FileMode.CreateNew );
        }
      }
    );
}