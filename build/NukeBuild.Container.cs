using System.Linq;
using Drift.Build.Utilities;
using HLabs.ImageReferences;
using HLabs.ImageReferences.Extensions.Nuke;
using JetBrains.Annotations;
using Nuke.Common;
using Nuke.Common.Tools.Docker;
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

  [Secret] //
  [Parameter( "DockerHubPassword - Required for releasing container images to Docker Hub" )]
  public readonly string DockerHubPassword;

  private static readonly PartialImageRef DriftImage = new("drift");
  private static readonly PartialImageRef LocalDriftImage = DriftImage.With( Registry.Localhost );
  private static readonly PartialImageRef DockerHubDriftImage = DriftImage.With( Registry.DockerHub, "hojmark" );
  [CanBeNull] private CanonicalImageRef CanonicalDriftImage = null;
  [CanBeNull] private ImageId DriftImageId = null;

  Target PublishContainer => _ => _
    .DependsOn( PublishBinaries, CleanArtifacts )
    .Requires( () => Commit )
    .Executes( async () => {
        using var _ = new OperationTimer( nameof(PublishContainer) );

        var version = await Versioning.Value.GetVersionAsync();

        Log.Information( "Building container image..." );
        // var created = DateTime.UtcNow.ToString( "o", CultureInfo.InvariantCulture ); // o = round-trip format / ISO 8601
        var output = DockerTasks.DockerBuild( s => s
          .SetPath( RootDirectory )
          .SetFile( "Containerfile" )
          .SetLabel(
            // Timestamping prevents the build from being idempotent
            // $"\"org.opencontainers.image.created={created}\"",
            $"\"org.opencontainers.image.version={version.WithoutMetadata()}\"",
            $"\"org.opencontainers.image.revision={Commit}\""
          )
        );

        var imageId = new ImageId( output.Last().Text );
        Log.Information( "Image ID is {ImageId}", imageId );

        Log.Information( "Tagging image..." );
        // For convenience, tag the image with dev
        var devTag = LocalDriftImage.Qualify( Tag.Dev );
        DockerTasks.DockerTag( s => s
          .SetSourceImage( imageId )
          .SetTargetImage( devTag )
        );

        // Determine canonical image reference
        var digest = imageId.GetDigest();
        DriftImageId = imageId;
        CanonicalDriftImage = LocalDriftImage.Canonicalize( digest );
        Log.Information( "Canonical reference is {ImageRef}", CanonicalDriftImage );
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

        var publicReferences = ( await Versioning.Value.Release!.GetImageReferences() )
          .OrderBy( LatestLast ) // Pushing 'latest' last will make sure it appears as the most recent tag on Docker Hub
          .ToArray();

        Push( DriftImageId, publicReferences.ToArray() );

        var registries = publicReferences.Select( r => r.Registry ).Distinct();

        Log.Information( "🐋 Released to {Registries}!", string.Join( " and ", registries ) );
      }
    );
/*
  /// <summary>
  /// Releases container image to public Docker Hub!
  /// </summary>
  Target PreReleaseContainer => _ => _
    .DependsOn( TestE2E )
    .Requires( () => DockerHubPassword )
    .Executes( async () => {
        using var _ = new OperationTimer( nameof(PreReleaseContainer) );

        var publicReferences = await Versioning.Value.Release!.GetImageReferences();

        Push( ImageIdDrift, publicReferences.ToArray() );

        var registries = publicReferences.DistinctBy( r => r.Registry );

        Log.Information( "🐋 Released to {Registries}!", string.Join( " and ", registries ) );
      }
    );*/

  private static int LatestLast( ImageRef r ) {
    return r.Tag == Tag.Latest ? 1 : 0;
  }

  private void Push( ImageId imageId, params QualifiedImageRef[] targets ) {
    var logInToDockerHub = targets.Any( r => r.Registry == Registry.DockerHub );

    try {
      if ( logInToDockerHub ) {
        DockerHubLogin();
      }

      Log.Debug(
        "Pushing {ImageId} to: {TargetTags}",
        imageId,
        string.Join( ", ", targets.Select( t => t.ToString() ) )
      );

      foreach ( var target in targets ) {
        // Unfortunately, Docker (unlike Podman) does not support pushing an image selected by image ID directly to a
        // registry tag (image ID → registry:tag). A local tag must be created first, which prevents this from being a
        // single, atomic operation.
        Log.Debug( "Tagging {ImageId} with {TargetTag}", DriftImageId, target );
        DockerTasks.DockerTag( s => s
          .SetSourceImage( DriftImageId )
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
      if ( logInToDockerHub ) {
        DockerHubLogout();
      }
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