using System;
using System.Linq;
using Drift.Build.Utilities;
using HLabs.ImageReferences;
using HLabs.ImageReferences.Extensions.Nuke;
using JetBrains.Annotations;
using Nuke.Common;
using Nuke.Common.Tools.Docker;
using Nuke.Common.Tools.DotNet;
using Serilog;
using Versioning;

// ReSharper disable VariableHidesOuterVariable
// ReSharper disable AllUnderscoreLocalParameterName
// ReSharper disable UnusedMember.Local

partial class NukeBuild {
  /*
  private static readonly string LocalImageName = "drift";

  private static readonly string DockerHubImageName = "hojmark/drift";
  */

  private static readonly string DockerHubUsername = "hojmark";

  [Parameter( "ContainerImageRef - Qualified image reference to push to Docker Hub with versioned tag" )]
  public readonly string ContainerImageRef = null;

  [Secret] //
  [Parameter( "DockerHubPassword - Required for releasing container images to Docker Hub" )]
  public readonly string DockerHubPassword;

  private static readonly PartialImageRef DriftImage = new("drift");
  private static readonly PartialImageRef LocalDriftImage = DriftImage.With( Registry.Localhost );
  private static readonly PartialImageRef DockerHubDriftImage = DriftImage.With( Registry.DockerHub, "hojmark" );
  [CanBeNull] private QualifiedImageRef _driftImageRef;

  Target BuildContainerImage => _ => _
    .DependsOn( PublishBinaries, CleanArtifacts )
    .OnlyWhenDynamic( () => Platform != DotNetRuntimeIdentifier.win_x64 )
    .Requires( () => Commit )
    .Executes( async () => {
        using var _ = new OperationTimer( nameof(BuildContainerImage) );

        var version = await Versioning.Value.GetVersionAsync();

        _driftImageRef = LocalDriftImage.Qualify( new Tag( $"staging.{Guid.NewGuid().ToString( "N" )}" ) );

        Log.Information( "Building container image..." );
        // var created = DateTime.UtcNow.ToString( "o", CultureInfo.InvariantCulture ); // o = round-trip format / ISO 8601
        DockerTasks.DockerBuild( s => s
          .SetPath( RootDirectory )
          .SetTag( _driftImageRef )
          .SetFile( "Containerfile" )
          .SetLabel(
            // Timestamping prevents the build from being idempotent
            // $"\"org.opencontainers.image.created={created}\"",
            $"\"org.opencontainers.image.version={version.WithoutMetadata()}\"",
            $"\"org.opencontainers.image.revision={Commit}\""
          )
        );

        Log.Information( "Built image: {ImageRef}", _driftImageRef );

        Log.Information( "Tagging image..." );
        // For convenience, tag the image with dev
        var devTag = LocalDriftImage.Qualify( Tag.Dev );
        DockerTasks.DockerTag( s => s
          .SetSourceImage( _driftImageRef )
          .SetTargetImage( devTag )
        );
      }
    );

  Target PushContainerImage => _ => _
    .DependsOn( TestE2E )
    .OnlyWhenDynamic( () => Platform != DotNetRuntimeIdentifier.win_x64 )
    .Requires( () => DockerHubPassword )
    .Executes( () => {
        using var _ = new OperationTimer( nameof(PushContainerImage) );

        var remoteRef = DockerHubDriftImage.Qualify( _driftImageRef!.Tag );

        Log.Information( "Pushing {Source} → {Target}", _driftImageRef, remoteRef );

        DockerHubLogin();
        try {
          DockerTasks.DockerTag( s => s
            .SetSourceImage( _driftImageRef )
            .SetTargetImage( remoteRef )
          );
          DockerTasks.DockerPush( s => s.SetName( remoteRef ) );
        }
        finally {
          DockerHubLogout();
        }

        // Expose the staging image ref as a job output so the release job can consume it via --containerimageref.
        GitHubActions.SetOutput( "container-image-ref", remoteRef.ToString() );

        Log.Information( "Container image ref: {Ref}", remoteRef );
      }
    );

  Target TagContainerImageForRelease => _ => _
    .OnlyWhenDynamic( () => Platform != DotNetRuntimeIdentifier.win_x64 )
    .Requires( () => DockerHubPassword )
    .Requires( () => ContainerImageRef )
    .Executes( async () => {
        using var _ = new OperationTimer( nameof(TagContainerImageForRelease) );

        var sourceRef = ContainerImageRef.QualifiedImage();

        var publicReferences = ( await Versioning.Value.Release!.GetImageReferences() )
          .OrderBy( LatestLast ) // Pushing 'latest' last ensures it appears as most recent on Docker Hub
          .ToArray();

        Log.Information( "Pulling {SourceRef}...", sourceRef );
        DockerHubLogin();
        try {
          DockerTasks.DockerPull( s => s.SetName( sourceRef.ToString() ) );
          Push( sourceRef, publicReferences );
        }
        finally {
          DockerHubLogout();
        }

        var registries = publicReferences.Select( r => r.Registry ).Distinct();
        Log.Information( "🐋 Released to {Registries}!", string.Join( " and ", registries ) );
      }
    );

  private static int LatestLast( ImageRef r ) {
    return r.Tag == Tag.Latest ? 1 : 0;
  }

  private static void Push( QualifiedImageRef source, params QualifiedImageRef[] targets ) {
    Log.Debug(
      "Pushing {Source} to: {Targets}",
      source,
      string.Join( ", ", targets.Select( t => t.ToString() ) )
    );

    foreach ( var target in targets ) {
      // Unfortunately, Docker (unlike Podman) does not support pushing an image selected by image ID directly to a
      // registry tag (image ID → registry:tag). A local tag must be created first, which prevents this from being a
      // single, atomic operation.
      Log.Debug( "Tagging {Source} with {Target}", source, target );
      DockerTasks.DockerTag( s => s
        .SetSourceImage( source )
        .SetTargetImage( target )
      );

      Log.Information( "Pushing {Target}", target );
      DockerTasks.DockerPush( s => s
        .SetName( target )
      );

      Log.Information( "Pushed {TargetTag}", target );
    }
  }

  private void DockerHubLogin() {
    Log.Debug( "Logging in to Docker Hub" );
    DockerTasks.DockerLogin( s => s
      .SetServer( Registry.DockerHub )
      .SetUsername( DockerHubUsername )
      .SetPassword( DockerHubPassword )
    );
  }

  private static void DockerHubLogout() {
    Log.Debug( "Logging out of Docker Hub" );
    DockerTasks.DockerLogout( s => s.SetServer( Registry.DockerHub ) );
  }
}