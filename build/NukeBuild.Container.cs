using System.Linq;
using Drift.Build.Utilities;
using Drift.Build.Utilities.ContainerImage;
using Nuke.Common;
using Nuke.Common.Tools.Docker;
using Serilog;

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
    .Executes( async () => {
        using var _ = new OperationTimer( nameof(PublishContainer) );

        // TODO use GetContainerImageReferences (.WithRepo(repo))
        var version = await Versioning.Value.GetVersionAsync();
        var localTagVersion = ImageReference.Localhost( "drift", version );

        // var created = DateTime.UtcNow.ToString( "o", CultureInfo.InvariantCulture ); // o = round-trip format / ISO 8601

        Log.Information( "Building container image {Tag}", localTagVersion );
        DockerTasks.DockerBuild( s => s
          .SetPath( RootDirectory )
          .SetFile( "Containerfile" )
          .SetTag( localTagVersion )
          .SetLabel(
            // Timestamping prevents the build from being idempotent
            // $"\"org.opencontainers.image.created={created}\"",
            $"\"org.opencontainers.image.version={version.WithoutMetadata()}\"",
            $"\"org.opencontainers.image.revision={Commit}\""
          )
        );

        // For convenience, tag the image with dev as well
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
    .Requires( () => DockerHubPassword ) // TODO require that login is successful
    .Executes( async () => {
        using var _ = new OperationTimer( nameof(ReleaseContainer) );

        var version = await Versioning.Value.GetVersionAsync();
        var local = ImageReference.Localhost( "drift", version );
        var publicc = await Versioning.Value.Release!.GetImageReferences();

        Push( local, publicc.ToArray() );

        var repos = publicc.Select( r => r.Repository ).Distinct();

        Log.Information( "🐋 Released to {Repositories}!", string.Join( " and ", repos ) );
      }
    );

  /// <summary>
  /// Releases container image to public Docker Hub!
  /// </summary>
  Target PreReleaseContainer => _ => _
    .DependsOn( TestE2E )
    .Requires( () => DockerHubPassword )
    .Executes( async () => {
        using var _ = new OperationTimer( nameof(PreReleaseContainer) );

        var version = await Versioning.Value.GetVersionAsync();
        var local = ImageReference.Localhost( "drift", version );
        var publicc = await Versioning.Value.Release!.GetImageReferences();

        Push( local, publicc.ToArray() );

        var repos = publicc.DistinctBy( r => r.Repository );

        Log.Information( "🐋 Released to {Repositories}!", string.Join( " and ", repos ) );
      }
    );

  private void Push( ImageReference source, params ImageReference[] targets ) {
    ImageReference[] allReferences = [source, ..targets];
    var loginToDockerHub = allReferences.Any( reference => reference.Host == DockerIoRegistry.Instance );

    try {
      if ( loginToDockerHub ) {
        DockerHubLogin();
      }

      Log.Debug(
        "Pushing {SourceTag} to: {TargetTags}",
        source,
        string.Join( ", ", targets.Select( t => t.ToString() ) )
      );

      foreach ( var target in targets ) {
        Log.Debug( "Re-tagging {SourceTag} -> {TargetTag}", source, target );
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
    Log.Debug( "Logging in to Docker Hub" );

    DockerTasks.DockerLogin( s => s
      .SetUsername( DockerHubUsername )
      .SetPassword( DockerHubPassword )
      .SetServer( DockerIoRegistry.Instance )
    );
  }

  private static void DockerHubLogout() {
    Log.Debug( "Logging out of Docker Hub" );

    DockerTasks.DockerLogout( s => s
      .SetServer( "docker.io" )
    );
  }
}