using System;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Nuke.Common;
using Nuke.Common.Git;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tools.DotNet;
using Octokit;
using Semver;
using Serilog;
using Utilities;
using Versioning;
using AuthenticationType = Octokit.AuthenticationType;
using Credentials = Octokit.Credentials;
using DotNetTestSettingsExtensions = Utilities.DotNetTestSettingsExtensions;
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

  [Parameter( $"{nameof(Commit)} - e.g. '4c16978aa41a3b435c0b2e34590f1759c1dc0763'" )]
  public string Commit = IsLocalBuild ? "0000000000000000000000000000000000000000" : null;

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

  internal GitHubClient GitHubClient {
    get {
      Credentials credentials;

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

  // TODO or is it build type? in that case, Default should probably be Other
  internal enum VersionStrategy {
    Default,
    Release,
    PreRelease
  }

  Target Version => _ => _
    .Before( BuildInfo )
    .DependentFor( Build, PublishBinaries, PublishContainer, Release, PreRelease )
    .Executes( async () => {
        using var _ = new TargetLifecycle( nameof(Version) );

        Log.Information( "Determining version..." );

        if ( ExecutionPlan.Contains( Release ) && ExecutionPlan.Contains( PreRelease ) ) {
          throw new InvalidOperationException(
            $"Execution plan cannot contain both {nameof(Release)} and {nameof(PreRelease)}"
          );
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
        var providedVersion = string.IsNullOrEmpty( CustomVersion ) ? "[none]" : CustomVersion;

        var builder = new StringBuilder();
        builder.AppendLine( $"Configuration        : {Configuration}" );
        builder.AppendLine( $"Version - provided   : {providedVersion}" );
        builder.AppendLine( $"Version - determined : {SemVer}" );
        builder.AppendLine( $"Tag Name             : {TagName}" );
        builder.AppendLine( $"Prerelease           : {SemVer.IsPrerelease}" );
        builder.AppendLine( $"Commit               : {Commit}" );

        Log.Information( "BUILD INFORMATION:\n{BuildInfo}", builder.ToString() );

        if ( AllowLocalRelease ) {
          Log.Warning( "Allowing locally built releases!" );
        }

        Log.Information(
          "MSBuild console output verbosity: {Verbosity} (parsed from {ParsedVerbosity})",
          MsBuildVerbosityParsed,
          MsBuildVerbosity
        );
      }
    );
}