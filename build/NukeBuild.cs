using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nuke.Common;
using Nuke.Common.Git;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.GitHub;
using Nuke.Common.Utilities.Collections;
using Octokit;
using Semver;
using Serilog;
using static Nuke.Common.Tools.DotNet.DotNetTasks;
using AuthenticationType = Octokit.AuthenticationType;
using Credentials = Octokit.Credentials;
using FileMode = System.IO.FileMode;
using ProductHeaderValue = Octokit.ProductHeaderValue;

// ReSharper disable AllUnderscoreLocalParameterName
// ReSharper disable UnusedMember.Local

class NukeBuild : Nuke.Common.NukeBuild {
  public static int Main() => Execute<NukeBuild>( x => x.Build );

  private static class Paths {
    internal static AbsolutePath PublishDirectory => RootDirectory / "publish";

    internal static AbsolutePath PublishDirectoryForRuntime( DotNetRuntimeIdentifier id ) =>
      PublishDirectory / id.ToString();

    internal static AbsolutePath ArtifactsDirectory => RootDirectory / "artifacts";
  }

  [Parameter( $"{nameof(Configuration)} - Configuration to build - Default is 'Debug' (local) or 'Release' (server)" )]
  public readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

  [Parameter( $"{nameof(CustomVersion)} - e.g. '3.1.5-preview.5'" )] //
  public readonly string CustomVersion;

  [Parameter( $"{nameof(Commit)} - e.g. '4c16978aa41a3b435c0b2e34590f1759c1dc0763'" )] //
  public string Commit;

  [Parameter( $"{nameof(Verbose)} - Verbose console output - Default is 'false'" )] // //
  public string Verbose = "normal";

  [Solution( GenerateProjects = true )] //
  private readonly Solution Solution;

  [GitRepository] // 
  private readonly GitRepository Repository;

  private const bool AllowLocalRelease = false;

  private static readonly DotNetRuntimeIdentifier[] SupportedRuntimes = [
    DotNetRuntimeIdentifier.linux_x64
    // TODO support more architectures
    /*, DotNetRuntimeIdentifier.linux_arm
      , DotNetRuntimeIdentifier.linux_arm64
      , DotNetRuntimeIdentifier.osx_x64 */
  ];

  private static readonly string[] FilesToDistribute = ["drift", "drift.dbg"];

  private const string BinaryLogName = "build.binlog";

  internal static GitHubClient GitHubClient {
    get {
      Credentials credentials;

      if ( EnvironmentInfo.GetVariable( "GITHUB_TOKEN" ) is { } token ) {
        credentials = new Credentials( token );
      }
      else {
        Log.Warning( "GITHUB_TOKEN environment variable not set. Using default credentials." );
        credentials = new Credentials(
          "blah",
          AuthenticationType.Oauth
        );
      }

      return new GitHubClient( new ProductHeaderValue( "hojmark-drift" ) ) { Credentials = credentials };
    }
  }

  private SemVersion SemVer {
    get;
    set;
  }

  private string TagName => "v" + SemVer.WithoutMetadata();

  // Insurance...
  private Target ExpectedTarget;

  // TODO Clean up release target and version validation
  private async Task ValidateAllowedReleaseTargetOrThrow( Target target ) {
    if ( ExpectedTarget != target ) {
      throw new InvalidOperationException(
        $"Target not allowed: {target}. Unexpected target. Did execution plan not contain Version?" );
    }

    if ( IsLocalBuild && !AllowLocalRelease ) {
      throw new InvalidOperationException(
        $"Target not allowed: {nameof(target)}. A local release build was prevented." );
    }

    if ( Configuration != Configuration.Release ) {
      throw new InvalidOperationException(
        $"Releases must be built with {nameof(Configuration)}.{nameof(Configuration.Release)}" );
    }

    if ( IsLocalBuild ) {
      Log.Warning( "⚠️ LOCAL RELEASE BUILD ⚠️" );
      Log.Warning( "Continuing in 10 seconds..." );
      await Task.Delay( TimeSpan.FromSeconds( 10 ) );
    }
  }

  //TODO or is it build type? in that case, Default should probably be Other
  public enum VersionStrategy {
    Default,
    Release,
    ReleaseSpecial
  }

  Target Version => _ => _
    .Before( BuildInfo )
    .DependentFor( Build, Publish, Release, ReleaseSpecial )
    .Executes( async () => {
        using var _ = new TargetLifecycle( nameof(Version) );

        Log.Information( "Determining version..." );

        if ( ExecutionPlan.Contains( Release ) && ExecutionPlan.Contains( ReleaseSpecial ) ) {
          throw new InvalidOperationException(
            $"Execution plan cannot contain both {nameof(Release)} and {nameof(ReleaseSpecial)}" );
        }

        if ( ExecutionPlan.Contains( Release ) ) {
          Log.Information( "Versioning strategy: {Strategy}", VersionStrategy.Release );
          SemVer = await VersionHelper.GetNextReleaseVersion( this, Repository );
          ExpectedTarget = Release;
        }
        else if ( ExecutionPlan.Contains( ReleaseSpecial ) ) {
          Log.Information( "Versioning strategy: {Strategy}", VersionStrategy.ReleaseSpecial );
          SemVer = VersionHelper.GetSpecialReleaseVersion( CustomVersion );
          ExpectedTarget = ReleaseSpecial;
        }
        else {
          Log.Information( "Versioning strategy: {Strategy}", VersionStrategy.Default );
          SemVer = VersionHelper.GetDefaultVersion();
        }

        if ( ExpectedTarget != null ) {
          await ValidateAllowedReleaseTargetOrThrow( ExpectedTarget );
        }
      }
    );


  Target BuildInfo => _ => _
    .Before( CleanProjects, Restore, CleanArtifacts )
    .DependsOn( Version )
    .DependentFor( Build )
    .Executes( () => {
        var builder = new StringBuilder();
        builder.AppendLine( $"Configuration        : {Configuration}" );
        builder.AppendLine( $"Version - provided   : {
          ( string.IsNullOrEmpty( CustomVersion ) ? "[none]" : CustomVersion )
        }" );
        builder.AppendLine( $"Version - determined : {SemVer}" );
        builder.AppendLine( $"Tag Name             : {TagName}" );
        builder.AppendLine( $"Prerelease           : {SemVer.IsPrerelease}" );
        builder.AppendLine( $"Commit               : {Commit}" );

        Log.Information( "BUILD INFORMATION:\n{BuildInfo}", builder.ToString() );

#pragma warning disable CS0162 // Unreachable code detected
        if ( AllowLocalRelease ) {
          Log.Warning( "Allowing locally built releases!" );
        }
#pragma warning restore CS0162 // Unreachable code detected

        if ( Verbose != "minimal" ) {
          Log.Warning( "Verbose console output enabled: " + Verbose );
        }
      }
    );

  Target CleanProjects => _ => _
    .Before( Restore )
    .Executes( () => {
        using var _ = new TargetLifecycle( nameof(CleanProjects) );

        var dirsToDelete = Solution.AllProjects
          .Where( project =>
            // Do not clean the build project
            project.Path != BuildProjectFile
          )
          .SelectMany( project => new[] {
            // Build dirs
            project.Directory / "bin", project.Directory / "obj",

            // trx logger
            project.Directory / "TestResults"
          } )
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
        using var _ = new TargetLifecycle( nameof(CleanArtifacts) );

        Log.Debug( "Cleaning {Directory}", Paths.PublishDirectory );
        Paths.PublishDirectory.CreateOrCleanDirectory();

        Log.Debug( "Cleaning {Directory}", Paths.ArtifactsDirectory );
        Paths.ArtifactsDirectory.CreateOrCleanDirectory();
      }
    );

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
          .SetBinaryLog( BinaryLogName )
          .EnableNoLogo()
          .EnableNoRestore()
        );
      }
    );

  Target CheckBuildWarnings => _ => _
    .After( Build )
    //.TriggeredBy( Build )
    .Executes( () => {
        using var _ = new TargetLifecycle( nameof(CheckBuildWarnings) );

        const string warningsLogName = "build-warnings.log";

        DotNetMSBuild( s => s
          .SetTargetPath( BinaryLogName )
          .SetNoConsoleLogger( true )
          .AddProcessAdditionalArguments( "-fl", $"-flp:logfile={warningsLogName};warningsonly" )
        );

        var warnings = File.ReadAllLines( warningsLogName );

        foreach ( var warning in warnings ) {
          Log.Information( warning );
        }

        var hasWarnings = warnings.Length != 0;

        if ( hasWarnings ) {
          Log.Error( "Found {Count} build warnings", warnings.Length );
          throw new Exception( $"Found {warnings.Length} build warnings" );
        }
        else {
          Log.Information( "🟢 No build warnings found" );
        }
      }
    );

  Target Test => _ => _
    .DependsOn( Build )
    .Executes( () => {
        using var _ = new TargetLifecycle( nameof(Test) );

        DotNetTest( s => s
          .SetProjectFile( Solution )
          .SetConfiguration( Configuration )
          .SetFilter( "Category!=E2E" )
          .ConfigureLoggers( Verbose )
          .EnableNoLogo()
          .EnableNoRestore()
          .EnableNoBuild()
        );
      }
    );

  Target Publish => _ => _
    .DependsOn( Build, CleanArtifacts )
    .Executes( () => {
        using var _ = new TargetLifecycle( nameof(Publish) );

        foreach ( var runtime in SupportedRuntimes ) {
          var publishDir = Paths.PublishDirectoryForRuntime( runtime );

          Log.Information( "Publishing {Runtime} build to {PublishDir}", runtime, publishDir );
          DotNetPublish( s => s
            .SetProject( Solution.Cli )
            .SetConfiguration( Configuration )
            .SetOutput( publishDir )
            .SetSelfContained( true )
            .SetVersionProperties( SemVer )
            //TODO if not specifying a RID, apparently only x64 gets built on x64 host
            .SetRuntime( runtime )
            .EnableNoLogo()
            .EnableNoRestore()
            .EnableNoBuild()
          );
        }
      }
    );

  Target E2ETest => _ => _
    .DependsOn( Publish )
    .After( Test )
    .Executes( () => {
      using var _ = new TargetLifecycle( nameof(E2ETest) );

      //TODO
      foreach ( var runtime in SupportedRuntimes ) {
        var publishDir = Paths.PublishDirectoryForRuntime( runtime );
        var driftBinary = publishDir / "drift";

        DotNetTest( s => s
          .SetProjectFile( Solution.Cli_E2ETests )
          .SetConfiguration( Configuration )
          .SetProcessEnvironmentVariable( "DRIFT_BINARY_PATH", driftBinary )
          .ConfigureLoggers( Verbose )
          .EnableNoLogo()
          .EnableNoRestore()
          .EnableNoBuild()
        );

        Log.Information( "Running E2E test on {Runtime} build to {PublishDir}", runtime, publishDir );
      }
    } );

  Target TestAll => _ => _
    .DependsOn( Test, E2ETest );

  Target TestAllLocal => _ => _
    .DependsOn( TestAll )
    .Executes( () => {
      //DotNetToolRestore();
      var result = ProcessTasks.StartProcess(
        "dotnet",
        "trx --verbosity verbose",
        workingDirectory: RootDirectory
      );
      result.AssertZeroExitCode();
    } );

  Target Pack => _ => _
    .DependsOn( Publish, CleanArtifacts )
    .Executes( () => {
        using var _ = new TargetLifecycle( nameof(Pack) );

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

  Target ReleaseSpecial => _ => _
    .Requires(
      // Version target CustomVersion parameter when this target is in the execution plan
      () => CustomVersion
    )
    .DependsOn( Pack, TestAll )
    .Executes( async () => {
      using var _ = new TargetLifecycle( nameof(ReleaseSpecial) );

      Log.Information( "🚨🌍🚢 RELEASING 🚢🌍🚨" );

      await ValidateAllowedReleaseTargetOrThrow( ReleaseSpecial );

      var release = await CreateDraftRelease();

      Log.Information( "⭐ Released {ReleaseName} to GitHub!", release.Name );
    } );


  Target Release => _ => _
    .DependsOn( Pack, TestAll )
    .Executes( async () => {
      using var _ = new TargetLifecycle( nameof(Release) );

      Log.Information( "🚨🌍🚢 RELEASING 🚢🌍🚨" );

      await ValidateAllowedReleaseTargetOrThrow( Release );

      var release = await CreateDraftRelease();

      await RemoveDraftStatus( release );

      Log.Information( "⭐ Released {ReleaseName} to GitHub!", release.Name );
    } );

  private async Task RemoveDraftStatus( Release release ) {
    var updateRelease = release.ToUpdate();
    updateRelease.Draft = false;

    Log.Information( "Removing release draft status..." );

    await GitHubClient.Repository.Release
      .Edit(
        Repository.GetGitHubOwner(),
        Repository.GetGitHubName(),
        release.Id,
        updateRelease
      );
  }

  //TODO make static
  private async Task<Release> CreateDraftRelease() {
    var newRelease = new NewRelease( TagName ) {
      Draft = true, Prerelease = true, Name = VersionHelper.CreateReleaseName( SemVer ), GenerateReleaseNotes = true
    };

    Log.Information( "Creating release {@Release}", newRelease );

    var release = await GitHubClient.Repository.Release.Create(
      Repository.GetGitHubOwner(),
      Repository.GetGitHubName(),
      newRelease
    );

    Log.Debug( "Created release {@Release}", release );

    Log.Information( "Uploading artifacts..." );

    foreach ( var artifact in Paths.ArtifactsDirectory.GetFiles() ) {
      var assetUpload = new ReleaseAssetUpload {
        FileName = artifact.Name, ContentType = "application/octet-stream", RawData = File.OpenRead( artifact )
      };

      Log.Information( "⬆️  Uploading {FileName}...", assetUpload.FileName );

      await GitHubClient.Repository.Release.UploadAsset( release, assetUpload );

      Log.Information( "✅ Uploaded {FileName}", assetUpload.FileName );
    }

    return await GitHubClient.Repository.Release.Get(
      Repository.GetGitHubOwner(),
      Repository.GetGitHubName(),
      release.Id
    );
  }
}