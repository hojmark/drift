using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Drift.Build.Utilities;
using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.Tools.GitHub;
using Octokit;
using Serilog;

// ReSharper disable VariableHidesOuterVariable
// ReSharper disable AllUnderscoreLocalParameterName
// ReSharper disable UnusedMember.Local

internal partial class NukeBuild {
  public bool AllowLocalRelease => true; // TODO set to false!

  public Target Release => _ => _
    .DependsOn( PackBinaries, ReleaseContainer, Test )
    .Executes( async () => {
        using var _ = new OperationTimer( nameof(Release) );

        Log.Information( "🚨🌍🚢 RELEASING 🚢🌍🚨" );

        //await ValidateAllowedReleaseTargetOrThrow( Release );

        var release = await CreateDraftRelease( prerelease: false );

        await RemoveDraftStatus( release );

        Log.Information( "⭐ Released {ReleaseName} to GitHub!", release.Name );
      }
    );

  public Target PreRelease => _ => _
    .DependsOn( PackBinaries, PreReleaseContainer, Test )
    .Executes( async () => {
        using var _ = new OperationTimer( nameof(PreRelease) );

        Log.Information( "🐋️ RELEASING 🐋️" );

        //await ValidateAllowedReleaseTargetOrThrow( PreRelease );

        var release = await CreateDraftRelease( prerelease: true );

        Log.Information( "⭐ Released {ReleaseName} to GitHub!", release.Name );
      }
    );


  // TODO Clean up release target and version validation
  private async Task ValidateAllowedReleaseTargetOrThrow( Target target ) {
    var existingTags =
      await GitHubClient.Repository.GetAllTags( Repository.GetGitHubOwner(), Repository.GetGitHubName() );
    var tag = await Versioning.Value.Release!.GetReleaseGitTagAsync();
    if ( existingTags.Any( t => t.Name == tag ) ) {
      throw new InvalidOperationException( $"Release {tag} already exists" );
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
    var newRelease = new NewRelease( await Versioning.Value.Release!.GetReleaseGitTagAsync() ) {
      Draft = true,
      Prerelease = prerelease,
      Name = await Versioning.Value.Release.GetReleaseNameAsync(),
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