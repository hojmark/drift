using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Drift.Build.Utilities.MsBuild;
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
using Utilities;
using Versioning;
using static Nuke.Common.Tools.DotNet.DotNetTasks;
using AuthenticationType = Octokit.AuthenticationType;
using Credentials = Octokit.Credentials;
using DotNetTestSettingsExtensions = Utilities.DotNetTestSettingsExtensions;
using FileMode = System.IO.FileMode;
using ProductHeaderValue = Octokit.ProductHeaderValue;

// ReSharper disable VariableHidesOuterVariable
// ReSharper disable AllUnderscoreLocalParameterName
// ReSharper disable UnusedMember.Local

[SuppressMessage(
  "StyleCop.CSharp.MaintainabilityRules",
  "SA1400:Access modifier should be declared",
  Justification = "Clutters the code and it's not critical in this class"
)]
[SuppressMessage(
  "StyleCop.CSharp.NamingRules",
  "SA1312:Variable names should begin with lower-case letter",
  Justification = "Clutters the code and it's not critical in this class"
)]
sealed partial class NukeBuild : Nuke.Common.NukeBuild {
  public static int Main() => Execute<NukeBuild>( x => x.Build );

  [Parameter( $"{nameof(Configuration)} - Configuration to build - Default is 'Debug' (local) or 'Release' (server)" )]
  public readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

  [Parameter( $"{nameof(CustomVersion)} - e.g. '3.1.5-preview.5'" )]
  public readonly string CustomVersion;

  [Required] //
  [Parameter( $"{nameof(Commit)} - e.g. '4c16978aa41a3b435c0b2e34590f1759c1dc0763'" )]
  public string Commit;

  [Parameter( $"{nameof(MsBuildVerbosity)} - Console output verbosity - Default is 'normal'" )]
  public string MsBuildVerbosity = Utilities.MsBuildVerbosity.Normal.ToMsBuildVerbosity();

  private MsBuildVerbosity MsBuildVerbosityParsed =>
    DotNetTestSettingsExtensions.FromMsBuildVerbosity( MsBuildVerbosity );

  [Solution( GenerateProjects = true )] //
  private readonly Solution Solution;

  [GitRepository] //
  private readonly GitRepository Repository;

  [Secret, Parameter( $"{nameof(GitHubToken)} - GitHub token used to create releases" )]
  public string GitHubToken;

  private const bool AllowLocalRelease = false;

  private const string BinaryBuildLogName = "build.binlog";
  private const string BinaryPublishLogName = "publish.binlog";

  private static readonly DotNetRuntimeIdentifier[] SupportedRuntimes = [
    DotNetRuntimeIdentifier.linux_x64,
    // TODO support more architectures
    /*
      , DotNetRuntimeIdentifier.linux_musl_x64
      , DotNetRuntimeIdentifier.linux_arm
      , DotNetRuntimeIdentifier.linux_arm64
      , DotNetRuntimeIdentifier.osx_x64
    */
  ];

  private static readonly string[] FilesToDistribute = ["drift", "drift.dbg"];

  internal GitHubClient GitHubClient {
    get {
      Credentials credentials;

      // if ( EnvironmentInfo.GetVariable( "GITHUB_TOKEN" ) is { } token ) {
      if ( GitHubToken is { } token ) {
        credentials = new Credentials( token );
      }
      else {
        // TODO update
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

  private static class Paths {
    internal static AbsolutePath PublishDirectory => RootDirectory / "publish";

    internal static AbsolutePath ArtifactsDirectory => RootDirectory / "artifacts";

    internal static AbsolutePath PublishDirectoryForRuntime( DotNetRuntimeIdentifier id ) =>
      PublishDirectory / id.ToString();
  }

  // Insurance...
  private Target ExpectedTarget;

  // TODO Clean up release target and version validation
  private async Task ValidateAllowedReleaseTargetOrThrow( Target target ) {
    if ( ExpectedTarget != target ) {
      throw new InvalidOperationException(
        $"Target not allowed: {target}. Unexpected target. Did execution plan not contain {nameof(Version)}?" );
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
      var delay = TimeSpan.FromSeconds( 10 );
      Log.Warning( "⚠️ LOCAL RELEASE BUILD ⚠️" );
      Log.Warning( "Continuing in {Delay} seconds...", (int) delay.TotalSeconds );
      await Task.Delay( delay );
    }

    var tags = await GitHubClient.Repository.GetAllTags(
      Repository.GetGitHubOwner(),
      Repository.GetGitHubName()
    );

    if ( tags.Any( t => t.Name == TagName ) ) {
      throw new InvalidOperationException( $"Release {TagName} already exists" );
    }
    else {
      Log.Debug( "Release {TagName} does not exist", TagName );
    }
  }

  // TODO or is it build type? in that case, Default should probably be Other
  internal enum VersionStrategy {
    Default,
    Release,
    PreRelease
  }

  Target Version => _ => _
    .Before( BuildInfo )
    .DependentFor( Build, PublishBinaries, Release, PreRelease )
    .Executes( async () => {
        using var _ = new TargetLifecycle( nameof(Version) );

        Log.Information( "Determining version..." );

        if ( ExecutionPlan.Contains( Release ) && ExecutionPlan.Contains( PreRelease ) ) {
          throw new InvalidOperationException(
            $"Execution plan cannot contain both {nameof(Release)} and {nameof(PreRelease)}" );
        }

        if ( ExecutionPlan.Contains( Release ) ) {
          Log.Information( "Versioning strategy: {Strategy}", VersionStrategy.Release );
          SemVer = await VersionHelper.GetNextReleaseVersion( this, Repository );
          ExpectedTarget = Release;
        }
        else if ( ExecutionPlan.Contains( PreRelease ) ) {
          Log.Information( "Versioning strategy: {Strategy}", VersionStrategy.PreRelease );
          SemVer = VersionHelper.GetPreReleaseVersion( CustomVersion );
          ExpectedTarget = PreRelease;
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

        Log.Information(
          "MSBuild console output verbosity: {Verbosity} (parsed from {ParsedVerbosity})",
          MsBuildVerbosityParsed,
          MsBuildVerbosity
        );
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

  Target Clean => _ => _
    .DependsOn( CleanProjects, CleanArtifacts );

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
          .SetBinaryLog( BinaryBuildLogName )
          .EnableNoLogo()
          .EnableNoRestore()
        );
      }
    );

  Target CheckBuildWarnings => _ => _
    .After( Build )
    // .TriggeredBy( Build )
    .Executes( () => {
        using var _ = new TargetLifecycle( nameof(CheckBuildWarnings) );

        var warnings = BinaryLogReader.GetWarnings( BinaryBuildLogName );

        foreach ( var warning in warnings ) {
          Log.Information( warning );
        }

        var hasWarnings = warnings.Length != 0;

        if ( hasWarnings ) {
          Log.Error( "Found {Count} build warnings", warnings.Length );
          throw new Exception( $"Found {warnings.Length} build warnings" );
        }

        Log.Information( "� No build warnings found" );
      }
    );

  Target CheckPublishWarnings => _ => _
    .After( CheckBuildWarnings, PublishBinaries )
    // .TriggeredBy( Build )
    .Executes( () => {
        using var _ = new TargetLifecycle( nameof(CheckPublishWarnings) );

        var warnings = BinaryLogReader.GetWarnings( BinaryPublishLogName );

        foreach ( var warning in warnings ) {
          Log.Information( warning );
        }

        var hasWarnings = warnings.Length != 0;

        if ( hasWarnings ) {
          Log.Error( "Found {Count} publish warnings", warnings.Length );
          throw new Exception( $"Found {warnings.Length} publish warnings" );
        }

        Log.Information( "� No publish warnings found" );
      }
    );

  Target CheckWarnings => _ => _
    .DependsOn( CheckBuildWarnings, CheckPublishWarnings )
    // .TriggeredBy( Publish )
    .Executes( () => {
        // using var _ = new TargetLifecycle( nameof(CheckWarnings) );
      }
    );

  Target TestUnit => _ => _
    .DependsOn( Build )
    .Executes( () => {
        using var _ = new TargetLifecycle( nameof(TestUnit) );

        DotNetTest( s => s
          .SetProjectFile( Solution )
          .SetConfiguration( Configuration )
          .SetFilter( "Category!=E2E&Category!=Container" )
          .ConfigureLoggers( MsBuildVerbosityParsed )
          .SetBlameHangTimeout( "60s" )
          .EnableNoLogo()
          .EnableNoRestore()
          .EnableNoBuild()
        );
      }
    );

  Target PublishBinaries => _ => _
    .DependsOn( Build, CleanArtifacts )
    .Executes( () => {
        using var _ = new TargetLifecycle( nameof(PublishBinaries) );

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

  Target TestE2E => _ => _
    .DependsOn( PublishBinaries )
    .After( TestUnit )
    .Executes( () => {
        using var _ = new TargetLifecycle( nameof(TestE2E) );

        // TODO
        foreach ( var runtime in SupportedRuntimes ) {
          var publishDir = Paths.PublishDirectoryForRuntime( runtime );
          var driftBinary = publishDir / "drift";

          DotNetTest( s => s
            .SetProjectFile( Solution.Cli_E2ETests )
            .SetConfiguration( Configuration )
            .SetProcessEnvironmentVariable( "DRIFT_BINARY_PATH", driftBinary )
            .ConfigureLoggers( MsBuildVerbosityParsed )
            .EnableNoLogo()
            .EnableNoRestore()
            .EnableNoBuild()
          );

          Log.Information( "Running E2E test on {Runtime} build to {PublishDir}", runtime, publishDir );
        }
      }
    );

  Target Test => _ => _
    .DependsOn( TestUnit, TestE2E, TestContainer );

  Target TestLocal => _ => _
    .DependsOn( Test )
    .Executes( () => {
        // DotNetToolRestore();
        var result = ProcessTasks.StartProcess(
          "dotnet",
          "trx --verbosity verbose",
          workingDirectory: RootDirectory
        );
        result.AssertZeroExitCode();
      }
    );

  Target PackBinaries => _ => _
    .DependsOn( PublishBinaries, CleanArtifacts )
    .Executes( () => {
        using var _ = new TargetLifecycle( nameof(PackBinaries) );

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

  Target Release => _ => _
    .DependsOn( PackBinaries, ReleaseContainer, Test )
    .Executes( async () => {
        using var _ = new TargetLifecycle( nameof(Release) );

        Log.Information( "��� RELEASING ���" );

        await ValidateAllowedReleaseTargetOrThrow( Release );

        var release = await CreateDraftRelease( prerelease: false );

        await RemoveDraftStatus( release );

        Log.Information( "⭐ Released {ReleaseName} to GitHub!", release.Name );
      }
    );

  Target PreRelease => _ => _
    .Requires(
      // Version target CustomVersion parameter when this target is in the execution plan
      () => CustomVersion
    )
    .DependsOn( PackBinaries, PreReleaseContainer, Test )
    .Executes( async () => {
        using var _ = new TargetLifecycle( nameof(PreRelease) );

        Log.Information( "�️ RELEASING �️" );

        await ValidateAllowedReleaseTargetOrThrow( PreRelease );

        var release = await CreateDraftRelease( prerelease: true );

        Log.Information( "⭐ Released {ReleaseName} to GitHub!", release.Name );
      }
    );

  private async Task RemoveDraftStatus( Release release ) {
    var updateRelease = release.ToUpdate();
    updateRelease.Draft = false;

    Log.Information( "Removing release draft status..." );

    await GitHubClient.Repository.Release.Edit(
      Repository.GetGitHubOwner(),
      Repository.GetGitHubName(),
      release.Id,
      updateRelease
    );
  }

  // TODO make static
  private async Task<Release> CreateDraftRelease( bool prerelease ) {
    var newRelease = new NewRelease( TagName ) {
      Draft = true,
      Prerelease = prerelease,
      Name = VersionHelper.CreateReleaseName( SemVer, includeMetadata: prerelease ),
      GenerateReleaseNotes = true
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