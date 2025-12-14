using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Drift.Build.Utilities;
using Drift.Build.Utilities.MsBuild;
using Drift.Build.Utilities.Versioning;
using Drift.Build.Utilities.Versioning.Abstractions;
using Nuke.Common;
using Nuke.Common.Git;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tools.DotNet;
using Octokit;
using Serilog;
using AuthenticationType = Octokit.AuthenticationType;
using Credentials = Octokit.Credentials;
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
sealed partial class NukeBuild : Nuke.Common.NukeBuild, INukeRelease {
  public NukeBuild() {
    Versioning = new Lazy<IVersioningStrategy>( () =>
      new VersioningStrategyFactory( this ).Create( Configuration, CustomVersion, GitHubClient, Repository )
    );
  }

  public static int Main() => Execute<NukeBuild>( x => x.Build );

  [Parameter( $"{nameof(Configuration)} - Configuration to build - Default is 'Debug' (local) or 'Release' (server)" )]
  public readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

  [Parameter( $"{nameof(CustomVersion)} - e.g. '3.1.5-preview.5'" )]
  public readonly string CustomVersion;

  [Parameter( $"{nameof(Commit)} - e.g. '4c16978aa41a3b435c0b2e34590f1759c1dc0763'" )]
  public string Commit = IsLocalBuild ? "0000000000000000000000000000000000000000" : null;

  [Parameter( $"{nameof(MsBuildVerbosity)} - Console output verbosity - Default is 'normal'" )]
  public string MsBuildVerbosity = Drift.Build.Utilities.MsBuild.MsBuildVerbosity.Normal.ToMsBuildVerbosity();

  private MsBuildVerbosity MsBuildVerbosityParsed =>
    MsBuildVerbosityExtensions.FromMsBuildVerbosity( MsBuildVerbosity );

  [Solution( GenerateProjects = true )] //
  private readonly Solution Solution;

  [GitRepository] //
  internal readonly GitRepository Repository;

  [Secret, Parameter( $"{nameof(GitHubToken)} - GitHub token used to create releases" )]
  public string GitHubToken;

  [Parameter( $"{nameof(Platform)} - A .NET RID but with underscores instead of dashes e.g. linux_x64 or win_x64" )]
  public DotNetRuntimeIdentifier Platform = IsLocalBuild
    ? RuntimeInformation.IsOSPlatform( OSPlatform.Linux )
      ? DotNetRuntimeIdentifier.linux_x64
      : RuntimeInformation.IsOSPlatform( OSPlatform.Windows )
        ? DotNetRuntimeIdentifier.win_x64
        : throw new PlatformNotSupportedException()
    : null;

  private static readonly DotNetRuntimeIdentifier[] SupportedRuntimes = [
    DotNetRuntimeIdentifier.linux_x64,
    DotNetRuntimeIdentifier.win_x64
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

  private Lazy<IVersioningStrategy> Versioning {
    get;
    init;
  }

  private static class Paths {
    internal static AbsolutePath PublishDirectory => RootDirectory / "publish";

    internal static AbsolutePath ArtifactsDirectory => RootDirectory / "artifacts";

    internal static AbsolutePath PublishDirectoryForRuntime( DotNetRuntimeIdentifier id ) =>
      PublishDirectory / id.ToString();
  }

  Target BuildInfo => _ => _
    .Before( CleanProjects, CleanArtifacts, Restore )
    .DependentFor( Build )
    .Executes( async () => {
        using var _ = new OperationTimer( nameof(BuildInfo) );

        Log.Information(
          "MSBuild console output verbosity is {Verbosity} (parsed from {ParsedVerbosity})",
          MsBuildVerbosityParsed,
          MsBuildVerbosity
        );

        var versionStrategy = Versioning.Value.GetType().Name.Replace( "Versioning", string.Empty );
        var versionProvided = string.IsNullOrEmpty( CustomVersion ) ? "[none]" : CustomVersion;
        var versionDetermined = await Versioning.Value.GetVersionAsync();

        var builder = new StringBuilder();
        // builder.AppendLine( $"Configuration        : {Configuration}" );
        builder.AppendLine( $"Version strategy     : {versionStrategy}" );
        builder.AppendLine( $"Version provided     : {versionProvided}" );
        builder.AppendLine( $"Version determined   : {versionDetermined}" );

        if ( Versioning.Value.Release is { } release ) {
          var name = await release.GetNameAsync();
          var gitTag = await release.GetGitTagAsync();
          var containerRefs = string.Join( ", ", await release.GetImageReferences() );

          builder.AppendLine( $"Release name         : {name}" );
          builder.AppendLine( $"Git tag              : {gitTag}" );
          builder.AppendLine( $"Container ref(s)     : {containerRefs}" );
        }

        Log.Information( "BUILD INFORMATION:\n{BuildInfo}", builder.ToString() );

        if ( Versioning.Value.Release != null && IsLocalBuild ) {
          var delay = TimeSpan.FromSeconds( 10 );
          Log.Warning( "⚠️ LOCAL RELEASE BUILD ⚠️" );
          Log.Warning( "Continuing in {Delay} seconds...", (int) delay.TotalSeconds );
          await Task.Delay( delay );
        }
      }
    );
}