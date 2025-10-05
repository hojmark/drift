using System;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.Docker;
using Nuke.Common.Tools.DotNet;
using Serilog;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

partial class NukeBuild {
  private static readonly string LocalImageName = "drift";

  private static readonly string DockerHubImageName = "hojmark/drift";
  private static readonly string DockerHubUsername = "hojmark";

  [Secret] //
  [Parameter( "DockerHubPassword - Required for releasing container images to Docker Hub" )]
  public readonly string DockerHubPassword;

  Target PublishContainer => _ => _
    .DependsOn( PublishBinaries, CleanArtifacts )
    //.Requires( () => SemVer )
    .Requires( () => Commit )
    .Executes( () => {
        using var _ = new TargetLifecycle( nameof(PublishContainer) );

        var localTagVersion = ContainerImageTag( ContainerRegistry.Local, TagType.Version );

        var created = DateTime.UtcNow.ToString( "o", CultureInfo.InvariantCulture ); // o = round-trip format / ISO 8601

        Log.Information( "Building container image {Tag}", localTagVersion );
        DockerTasks.DockerBuild( s => s
          .SetPath( RootDirectory )
          .SetFile( "Containerfile" )
          .SetTag( localTagVersion )
          .SetLabel(
            // Timestamping prevents build from being idempotent
            //$"\"org.opencontainers.image.created={created}\"",
            $"\"org.opencontainers.image.version={SemVer.ToString()}\"",
            $"\"org.opencontainers.image.revision={Commit}\""
          )
        );

        var localTagDev = ContainerImageTag( ContainerRegistry.Local, TagType.Dev );

        Log.Information( "Re-tagging {LocalTagVersion} -> {LocalTagDev}", localTagVersion, localTagDev );
        DockerTasks.DockerTag( s => s
          .SetSourceImage( localTagVersion )
          .SetTargetImage( localTagDev )
        );
      }
    );


  Target TestContainer => _ => _
    .DependsOn( PublishContainer )
    .After( TestUnit, TestE2E )
    .Executes( () => {
        using var _ = new TargetLifecycle( nameof(TestContainer) );

        DotNetTest( s => s
          .SetProjectFile( Solution.Cli_ContainerTests )
          .SetConfiguration( Configuration )
          .SetProcessEnvironmentVariable( "DRIFT_CONTAINER_IMAGE_TAG",
            ContainerImageTag( ContainerRegistry.Local, TagType.Version )
          )
          .ConfigureLoggers( Verbose )
          .EnableNoLogo()
          .EnableNoRestore()
          .EnableNoBuild()
        );
      }
    );

  Target TestContainer2 => _ => _
    .DependsOn( PublishContainer )
    .Executes( () => {
        using var _ = new TargetLifecycle( nameof(TestContainer2) );

        //TODO may need to pull image from docker hub if not present locally

        var tag = ContainerImageTag( ContainerRegistry.Local, TagType.Version );
        ( Paths.PublishDirectory / "container" ).CreateOrCleanDirectory();

        return;
        var output = DockerTasks.DockerImageInspect( options => options.SetImages( "drift:latest" ) );

        var jsonText = string.Join( Environment.NewLine, output.Select( o => o.Text ) );

        var json = JsonDocument.Parse( jsonText );
        var config = json.RootElement[0].GetProperty( "Config" );
        var labels = config.GetProperty( "Labels" );

        var version = labels.GetProperty( "org.opencontainers.image.version" ).GetString();
        var revision = labels.GetProperty( "org.opencontainers.image.revision" ).GetString();

        //Log.Information( "Image version: {version}, revision: {revision}", version, revision );
      }
    );

  /// <summary>
  /// Releases container image to public Docker Hub!
  /// </summary>
  // ReSharper disable once UnusedMember.Local
  Target ReleaseContainer => _ => _
    // .DependsOn( Publish )
    .Requires( () => DockerHubPassword )
    //.Requires( () => SemVer )
    .DependsOn( TestContainer )
    .Executes( () => {
        Log.Information( "Logging in to Docker Hub" );
        DockerTasks.DockerLogin( c => c
          .SetUsername( DockerHubUsername )
          .SetPassword( DockerHubPassword )
          .SetServer( "docker.io" )
        );

        var localTag = ContainerImageTag( ContainerRegistry.Local, TagType.Version );
        string[] dockerHubTags = [
          ContainerImageTag( ContainerRegistry.DockerHub, TagType.Version ),
          ContainerImageTag( ContainerRegistry.DockerHub, TagType.Latest )
        ];

        Log.Information(
          "Pushing {LocalTag} to Docker with new tags: {DockerHubTags}", localTag,
          string.Join( ", ", dockerHubTags )
        );

        foreach ( var dockerHubTag in dockerHubTags ) {
          Log.Information( "Re-tagging {LocalTag} -> {DockerHubTag}", localTag, dockerHubTag );
          DockerTasks.DockerTag( s => s
            .SetSourceImage( localTag )
            .SetTargetImage( dockerHubTag )
          );

          Log.Information( "Pushing {DockerHubTag} to Docker Hub", dockerHubTag );
          DockerTasks.DockerPush( s => s
            .SetName( dockerHubTag )
          );
        }

        Log.Information( "Logging out of Docker Hub" );
        DockerTasks.DockerLogout( s => s
          .SetServer( "docker.io" )
        );
      }
    );

  private enum ContainerRegistry {
    Local,
    DockerHub
  }

  internal sealed class DockerImageName {
    public string Registry {
      get;
      set;
    } = "docker.io"; // default

    public required string Namespace {
      get;
      set;
    } = "hojmark"; // default if omitted

    public required string Repository {
      get;
      set;
    } = "drift";

    public required string Tag {
      get;
      set;
    } = "latest"; // default

    public override string ToString() => $"";
  }

  internal enum TagType {
    Version,
    Latest,
    Dev
  }

  private string ContainerImageTag( ContainerRegistry registry, TagType tagType ) {
    var imageName = registry switch {
      ContainerRegistry.DockerHub => DockerHubImageName,
      ContainerRegistry.Local => LocalImageName,
      _ => throw new ArgumentOutOfRangeException( nameof(registry), registry, null )
    };

    var tag = tagType switch {
      TagType.Version => SemVer.ToString(),
      TagType.Latest => "latest",
      TagType.Dev => "dev",
      _ => throw new ArgumentOutOfRangeException( nameof(tagType), tagType, null )
    };

    return $"{imageName}:{tag}";
  }
}