using System.IO;
using System.Linq;
using Drift.Build.Utilities;
using Nuke.Common.IO;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Serilog;
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
    .OnlyWhenStatic( () => SupportedRuntimes.Contains( Platform ) )
    .Requires( () => Platform )
    .Executes( async () => {
        using var _ = new OperationTimer( nameof(PublishBinaries) );

        var publishDir = Paths.PublishDirectoryForRuntime( Platform );
        var version = await Versioning.Value.GetVersionAsync();

        Log.Information( "Publishing {Runtime} build to {PublishDir}", Platform, publishDir );
        Log.Debug( "Supported runtimes are {SupportedRuntimes}", string.Join( ", ", SupportedRuntimes ) );
        DotNetPublish( s => s
          .SetProject( Solution.Cli )
          .SetConfiguration( Configuration )
          .SetOutput( publishDir )
          .SetSelfContained( true )
          .SetVersionProperties( version )
          .SetRuntime( Platform )
          .SetProcessAdditionalArguments( $"-bl:{BinaryPublishLogName}" )
          .EnableNoLogo()
          .EnableNoRestore()
          .EnableNoBuild()
        );
      }
    );

  Target PackBinaries => _ => _
    .DependsOn( PublishBinaries, CleanArtifacts )
    .Executes( async () => {
        using var _ = new OperationTimer( nameof(PackBinaries) );

        var version = await Versioning.Value.GetVersionAsync();

        foreach ( var runtime in SupportedRuntimes ) {
          var publishDir = Paths.PublishDirectoryForRuntime( runtime );
          var artifactFile = Paths.ArtifactsDirectory / $"drift_{version.WithoutMetadata()}_{runtime}.tar.gz";

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