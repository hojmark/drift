using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.Tools.GitHub;
using Octokit;
using Serilog;
using Utilities;
using Versioning;

// ReSharper disable VariableHidesOuterVariable
// ReSharper disable AllUnderscoreLocalParameterName
// ReSharper disable UnusedMember.Local

internal partial class NukeBuild {
  private const bool AllowLocalRelease = false;

  // Insurance...
  private Target ExpectedTarget;

  Target Release => _ => _
    .DependsOn( PackBinaries, ReleaseContainer, Test )
    .Executes( async () => {
        using var _ = new OperationTimer( nameof(Release) );

        Log.Information( "🚨🌍🚢 RELEASING 🚢🌍🚨" );

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
        using var _ = new OperationTimer( nameof(PreRelease) );

        Log.Information( "🐋️ RELEASING 🐋️" );

        await ValidateAllowedReleaseTargetOrThrow( PreRelease );

        var release = await CreateDraftRelease( prerelease: true );

        Log.Information( "⭐ Released {ReleaseName} to GitHub!", release.Name );
      }
    );


  // TODO Clean up release target and version validation
  private async Task ValidateAllowedReleaseTargetOrThrow( Target target ) {
    if ( ExpectedTarget != target ) {
      throw new InvalidOperationException(
        $"Target not allowed: {target}. Unexpected target. Did execution plan not contain {nameof(Version)}?"
      );
    }

    if ( IsLocalBuild && !AllowLocalRelease ) {
      throw new InvalidOperationException(
        $"Target not allowed: {nameof(target)}. A local release build was prevented."
      );
    }

    if ( Configuration != Configuration.Release ) {
      throw new InvalidOperationException(
        $"Releases must be built with {nameof(Configuration)}.{nameof(Configuration.Release)}"
      );
    }

    var tags = await GitHubClient.Repository.GetAllTags( Repository.GetGitHubOwner(), Repository.GetGitHubName() );

    if ( tags.Any( t => t.Name == TagName ) ) {
      throw new InvalidOperationException( $"Release {TagName} already exists" );
    }

    if ( IsLocalBuild ) {
      var delay = TimeSpan.FromSeconds( 10 );
      Log.Warning( "⚠️ LOCAL RELEASE BUILD ⚠️" );
      Log.Warning( "Continuing in {Delay} seconds...", (int) delay.TotalSeconds );
      await Task.Delay( delay );
    }
  }

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