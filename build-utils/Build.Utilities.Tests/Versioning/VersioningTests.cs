using System.Text.RegularExpressions;
using Drift.Build.Utilities.Tests.NukeBuild;
using Drift.Build.Utilities.Versioning;
using Drift.Build.Utilities.Versioning.Strategies;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Nuke.Common;
using Nuke.Common.Git;
using Octokit;
using Assert = TUnit.Assertions.Assert;

namespace Drift.Build.Utilities.Tests.Versioning;

internal sealed class VersioningTests {
  [Test]
  public async Task DefaultVersioningVersionTest() {
    // Arrange
    var build = new TestNukeBuild();

    // Act
    var strategy = new DefaultVersioning( build );
    var version = await strategy.GetVersionAsync();

    // Assert
    using ( Assert.Multiple() ) {
      await Assert.That( version.ToString() ).IsEqualTo( "0.0.0-local" ).Or.EqualTo( "0.0.0-ci" );
      await Assert.That( strategy.Release ).IsNull();
    }
  }

  [Test]
  public async Task DefaultVersioningWhenNoReleaseTargets() {
    // Arrange
    var build = new NukeBuildWithArbitraryTarget().WithExecutionPlan( b => b.Arbitrary );

    // Act
    var factory = new VersioningStrategyFactory( build );
    var strategy = factory.Create( Configuration.Debug, null, null, null, null );
    var version = await strategy.GetVersionAsync();

    // Assert
    using ( Assert.Multiple() ) {
      await Assert.That( version.ToString() ).IsEqualTo( "0.0.0-local" ).Or.EqualTo( "0.0.0-ci" );
      await Assert.That( strategy.Release ).IsNull();
    }
  }

  [Test]
  public async Task MultipleReleaseTargetsInPlanThrows() {
    // Arrange
    var build = new TestNukeBuild().WithExecutionPlan( b => b.CreateRelease, b => b.CreatePreRelease );

    // Act
    var factory = new VersioningStrategyFactory( build );
    var exception = Assert.Throws<InvalidOperationException>( () =>
      factory.Create( Configuration.Release, null, null, null, null )
    );

    // Assert
    await Assert.That( exception.Message ).IsEqualTo(
      $"Execution plan cannot contain both {nameof(TestNukeBuild.CreateRelease)} and {nameof(TestNukeBuild.CreatePreRelease)}"
    );
  }

  public static IEnumerable<Func<TestNukeBuild, (TestNukeBuild Build, string ExpectedMessageContains)>>
    MismatchedReleaseTypeCases() {
    yield return b => (
      b.WithExecutionPlan( x => x.CreateRelease ).WithReleaseType( ReleaseType.None ),
      "CreateRelease requires ReleaseType to be Release but got None"
    );
    yield return b => (
      b.WithExecutionPlan( x => x.CreatePreRelease ).WithReleaseType( ReleaseType.None ),
      "CreatePreRelease requires ReleaseType to be PreRelease but got None"
    );
    yield return b => (
      b.WithExecutionPlan( x => x.CreateRelease ).WithReleaseType( ReleaseType.PreRelease ),
      "CreateRelease requires ReleaseType to be Release but got PreRelease"
    );
    yield return b => (
      b.WithExecutionPlan( x => x.CreatePreRelease ).WithReleaseType( ReleaseType.Release ),
      "CreatePreRelease requires ReleaseType to be PreRelease but got Release"
    );
  }

  [Test]
  [MethodDataSource( nameof(MismatchedReleaseTypeCases) )]
  public async Task MismatchedReleaseTypeThrows(
    Func<TestNukeBuild, (TestNukeBuild Build, string ExpectedMessageContains)> caseFactory
  ) {
    // Arrange
    var (build, expectedMessageContains) = caseFactory( new TestNukeBuild() );

    // Act
    var factory = new VersioningStrategyFactory( build );
    var exception = Assert.Throws<InvalidOperationException>( () =>
      factory.Create( Configuration.Release, null, null, null, null )
    );

    // Assert
    Console.WriteLine( exception.Message );
    await Assert.That( exception.Message ).Contains( expectedMessageContains );
  }

  [Test]
  public void PreReleaseWithoutVersionThrows() {
    // Arrange
    var build = new TestNukeBuild()
      .WithExecutionPlan( b => b.CreatePreRelease )
      .WithReleaseType( ReleaseType.PreRelease );

    // Act
    var factory = new VersioningStrategyFactory( build );
    var strategy = factory.Create( Configuration.Release, null, null, null, null );

    // Assert
    Assert.ThrowsAsync<InvalidOperationException>( async () => await strategy.GetVersionAsync() );
  }

  public static IEnumerable<Func<TestNukeBuild, (TestNukeBuild Build, string? PrereleaseIdentifiers)>>
    DebugConfigurationCases() {
    yield return b => (
      b.WithExecutionPlan( x => x.CreatePreRelease ).WithReleaseType( ReleaseType.PreRelease ),
      "custom"
    );
    yield return b => (
      b.WithExecutionPlan( x => x.CreateRelease ).WithReleaseType( ReleaseType.Release ),
      null
    );
  }

  [Test]
  [MethodDataSource( nameof(DebugConfigurationCases) )]
  public void DebugConfigurationThrows(
    Func<TestNukeBuild, (TestNukeBuild Build, string? PrereleaseIdentifiers)> caseFactory ) {
    // Arrange
    var (build, prereleaseIdentifiers) = caseFactory( new TestNukeBuild() );

    // Act
    var factory = new VersioningStrategyFactory( build );

    // Assert
    Assert.Throws<InvalidOperationException>( () =>
      factory.Create( Configuration.Debug, prereleaseIdentifiers, null, null, null )
    );
  }

  [Test]
  public async Task PreReleaseValid() {
    // Arrange
    var build = new TestNukeBuild()
      .WithExecutionPlan( b => b.CreatePreRelease )
      .WithReleaseType( ReleaseType.PreRelease );

    // Act
    var factory = new VersioningStrategyFactory( build );
    var strategy = factory.Create( Configuration.Release, "custom", null, null, null );
    var version = await strategy.GetVersionAsync();

    // Assert
    using ( Assert.Multiple() ) {
      await Assert.That( version.ToString() ).StartsWith( "0.0.0-custom." );
      // 14-digit timestamp is appended
      await Assert.That( version.PrereleaseIdentifiers[version.PrereleaseIdentifiers.Count - 1].ToString() )
        .Matches( new Regex( @"^\d{14}$" ) );
    }
  }

  [Test]
  public void PreReleaseRejectsFullSemVerString() {
    // Arrange
    var build = new TestNukeBuild()
      .WithExecutionPlan( b => b.CreatePreRelease )
      .WithReleaseType( ReleaseType.PreRelease );

    // Act
    var factory = new VersioningStrategyFactory( build );
    var strategy = factory.Create( Configuration.Release, "0.0.0-custom", null, null, null );

    // Assert
    // Full semver strings must be rejected — only dot-separated prerelease identifiers are accepted
    Assert.ThrowsAsync<InvalidOperationException>( async () => await strategy.GetVersionAsync() );
  }

  [Test]
  public async Task PreReleaseVersionIsTemporallyConsistent() {
    // Arrange
    var timeProvider = new FakeTimeProvider();
    var build = new TestNukeBuild()
      .WithExecutionPlan( b => b.CreatePreRelease )
      .WithReleaseType( ReleaseType.PreRelease );

    // Act
    var factory = new VersioningStrategyFactory( build, timeProvider );
    var strategy = factory.Create( Configuration.Release, "custom", null, null, null );
    var versionBefore = await strategy.GetVersionAsync();
    timeProvider.Advance( TimeSpan.FromSeconds( 2 ) );
    var versionAfter = await strategy.GetVersionAsync();

    // Assert
    await Assert.That( versionBefore ).IsEqualTo( versionAfter );
  }

  [Test]
  public void ReleaseWithPrereleaseIdentifiersThrows() {
    // Arrange
    var build = new TestNukeBuild()
      .WithExecutionPlan( b => b.CreateRelease )
      .WithReleaseType( ReleaseType.Release );

    // Act
    var factory = new VersioningStrategyFactory( build );

    // Assert
    Assert.Throws<InvalidOperationException>( () =>
      factory.Create( Configuration.Release, "custom", null, null, null )
    );
  }

  [Test]
  public async Task ReleaseValid() {
    // Arrange
    var build = new TestNukeBuild()
      .WithExecutionPlan( b => b.CreateRelease )
      .WithReleaseType( ReleaseType.Release );

    var gitRepository = GitRepository.FromUrl( "https://github.com/test-owner/test-repo" );

    var gitHubClient = Substitute.For<IGitHubClient>();
    var releasesClient = Substitute.For<IReleasesClient>();

    var latestRelease = new Release(
      url: "https://api.github.com/repos/test-owner/test-repo/releases/1",
      htmlUrl: "https://github.com/test-owner/test-repo/releases/tag/v0.0.0-alpha.1",
      assetsUrl: "https://api.github.com/repos/test-owner/test-repo/releases/1/assets",
      uploadUrl: "https://uploads.github.com/repos/test-owner/test-repo/releases/1/assets{?name,label}",
      id: 1,
      nodeId: "MDc6UmVsZWFzZTE=",
      tagName: "v0.0.0-alpha.1",
      targetCommitish: "main",
      name: "v0.0.0-alpha.1",
      body: "Test release",
      draft: false,
      prerelease: true,
      createdAt: DateTimeOffset.UtcNow.AddDays( -1 ),
      publishedAt: DateTimeOffset.UtcNow.AddDays( -1 ),
      author: null!,
      tarballUrl: "https://api.github.com/repos/test-owner/test-repo/tarball/v0.0.0-alpha.1",
      zipballUrl: "https://api.github.com/repos/test-owner/test-repo/zipball/v0.0.0-alpha.1",
      assets: []
    );

    releasesClient.GetLatest( "test-owner", "test-repo" ).Returns( latestRelease );
    gitHubClient.Repository.Release.Returns( releasesClient );

    // Act
    var factory = new VersioningStrategyFactory( build );
    var strategy = factory.Create( Configuration.Release, null, null, gitHubClient, gitRepository );
    var version = await strategy.GetVersionAsync();

    // Assert
    await Assert.That( version.ToString() ).IsEqualTo( "0.0.0-alpha.2" );
  }

  [Test]
  public async Task ExactVersioningValid() {
    // Arrange
    var build = new TestNukeBuild()
      .WithExecutionPlan( b => b.CreatePreRelease )
      .WithReleaseType( ReleaseType.PreRelease );

    // Act
    var factory = new VersioningStrategyFactory( build );
    var strategy = factory.Create( Configuration.Release, null, "0.0.0-windows.10.20260319202632", null, null );
    var version = await strategy.GetVersionAsync();

    // Assert: exact version returned verbatim
    await Assert.That( version.ToString() ).IsEqualTo( "0.0.0-windows.10.20260319202632" );
  }

  [Test]
  public void ExactVersioningWithNullThrows() {
    // Arrange
    var strategy = new ExactVersioning( Configuration.Release, "   ", null!, null! );

    // Assert
    Assert.ThrowsAsync<InvalidOperationException>( async () => await strategy.GetVersionAsync() );
  }

  [Test]
  public async Task ExactVersioningValidWithReleaseVersion() {
    // Arrange
    var build = new TestNukeBuild()
      .WithExecutionPlan( b => b.CreateRelease )
      .WithReleaseType( ReleaseType.Release );

    // Act
    var factory = new VersioningStrategyFactory( build );
    var strategy = factory.Create( Configuration.Release, null, "1.5.0", null, null );
    var version = await strategy.GetVersionAsync();

    // Assert
    await Assert.That( strategy.GetType() ).IsEqualTo( typeof(ExactVersioning) );
    await Assert.That( version.ToString() ).IsEqualTo( "1.5.0" );
  }
}

internal sealed class NukeBuildWithArbitraryTarget : TestNukeBuild {
  // Justification: NUKE Target properties are instance properties by convention; static is not valid here
#pragma warning disable S2325
  public Target Arbitrary => _ => _
#pragma warning restore S2325
    .Executes( () => {
      }
    );
}