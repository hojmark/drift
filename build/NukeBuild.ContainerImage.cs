using System.Linq;
using Drift.Build.Utilities.ContainerImage;
using Nuke.Common;
using Nuke.Common.Tools.Docker;
using Serilog;
using Utilities;

// ReSharper disable VariableHidesOuterVariable
// ReSharper disable AllUnderscoreLocalParameterName
// ReSharper disable UnusedMember.Local

partial class NukeBuild {
  /*
  private static readonly string LocalImageName = "drift";

  private static readonly string DockerHubImageName = "hojmark/drift";
  */


  private static readonly string DockerHubUsername = "hojmark";

  [Secret] //
  [Parameter( "DockerHubPassword - Required for releasing container images to Docker Hub" )]
  public readonly string DockerHubPassword;

  Target PublishContainer => _ => _
    .DependsOn( PublishBinaries, CleanArtifacts )
    .Requires( () => Commit )
    .Executes( () => {
        using var _ = new OperationTimer( nameof(PublishContainer) );

        var localTagVersion = ImageReference.Localhost( "drift", SemVer );

        // var created = DateTime.UtcNow.ToString( "o", CultureInfo.InvariantCulture ); // o = round-trip format / ISO 8601

        Log.Information( "Building container image {Tag}", localTagVersion );
        DockerTasks.DockerBuild( s => s
          .SetPath( RootDirectory )
          .SetFile( "Containerfile" )
          .SetTag( localTagVersion )
          .SetLabel(
            // Timestamping prevents build from being idempotent
            // $"\"org.opencontainers.image.created={created}\"",
            $"\"org.opencontainers.image.version={SemVer.ToString()}\"",
            $"\"org.opencontainers.image.revision={Commit}\""
          )
        );

        var localTagDev = localTagVersion.WithTag( DevVersion.Instance );

        Log.Information( "Re-tagging {LocalTagVersion} -> {LocalTagDev}", localTagVersion, localTagDev );
        DockerTasks.DockerTag( s => s
          .SetSourceImage( localTagVersion )
          .SetTargetImage( localTagDev )
        );
      }
    );

  /// <summary>
  /// Releases container image to public Docker Hub!
  /// </summary>
  Target ReleaseContainer => _ => _
    .DependsOn( TestE2E )
    .Requires( () => DockerHubPassword )
    .Executes( () => {
        using var _ = new OperationTimer( nameof(ReleaseContainer) );

        var local = ImageReference.Localhost( "drift", SemVer );
        var dockerHub = new[] {
          ImageReference.DockerIo( "hojmark", "drift", SemVer ),
          ImageReference.DockerIo( "hojmark", "drift", LatestVersion.Instance )
        };

        Push( local, dockerHub );

        Log.Information( "🐋 Released {ImageReference} to Docker Hub!", dockerHub.Last() );
      }
    );

  /// <summary>
  /// Releases container image to public Docker Hub!
  /// </summary>
  Target PreReleaseContainer => _ => _
    .DependsOn( TestE2E )
    .Requires( () => DockerHubPassword )
    .Executes( () => {
        using var _ = new OperationTimer( nameof(PreReleaseContainer) );

        var local = ImageReference.Localhost( "drift", SemVer );
        var dockerHub = ImageReference.DockerIo( "hojmark", "drift", SemVer );

        Push( local, dockerHub );

        Log.Information( "🐋 Released {ImageReference} to Docker Hub!", dockerHub );
      }
    );

  private void Push( ImageReference source, params ImageReference[] targets ) {
    ImageReference[] allReferences = [source, ..targets];
    var loginToDockerHub = allReferences.Any( reference => reference.Host == DockerIoRegistry.Instance );

    try {
      if ( loginToDockerHub ) {
        DockerHubLogin();
      }

      Log.Information(
        "Pushing {SourceTag} to: {TargetTags}",
        source,
        string.Join( ", ", targets.Select( t => t.ToString() ) )
      );

      foreach ( var target in targets ) {
        Log.Information( "Re-tagging {SourceTag} -> {TargetTag}", source, target );
        DockerTasks.DockerTag( s => s
          .SetSourceImage( source )
          .SetTargetImage( target )
        );

        Log.Information( "Pushing {TargetTag}", target );
        DockerTasks.DockerPush( s => s
          .SetName( target )
        );
        Log.Information( "Pushed {TargetTag}", target );
      }
    }
    finally {
      if ( loginToDockerHub ) {
        DockerHubLogout();
      }
    }
  }

  private void DockerHubLogin() {
    Log.Information( "Logging in to Docker Hub" );

    DockerTasks.DockerLogin( s => s
      .SetUsername( DockerHubUsername )
      .SetPassword( DockerHubPassword )
      .SetServer( DockerIoRegistry.Instance )
    );
  }

  private static void DockerHubLogout() {
    Log.Information( "Logging out of Docker Hub" );

    DockerTasks.DockerLogout( s => s
      .SetServer( "docker.io" )
    );
  }
}