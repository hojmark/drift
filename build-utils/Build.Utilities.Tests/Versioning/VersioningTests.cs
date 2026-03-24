using System.Text.RegularExpressions;
using Drift.Build.Utilities.Tests.NukeBuild;
using Drift.Build.Utilities.Versioning;
using Drift.Build.Utilities.Versioning.Strategies;
using Nuke.Common;
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
      factory.Create( Configuration.Debug, prereleaseIdentifiers, null, null, null ) );
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

    // Assert: prefix is preserved and a 14-digit timestamp is appended
    using ( Assert.Multiple() ) {
      await Assert.That( version.ToString() ).StartsWith( "0.0.0-custom." );
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

    // Assert: full semver strings must be rejected — only dot-separated identifiers are accepted
    Assert.ThrowsAsync<InvalidOperationException>( async () => await strategy.GetVersionAsync() );
  }

  [Test]
  public async Task PreReleaseVersionIsTemporallyConsistent() {
    // Arrange
    var build = new TestNukeBuild()
      .WithExecutionPlan( b => b.CreatePreRelease )
      .WithReleaseType( ReleaseType.PreRelease );

    // Act
    var factory = new VersioningStrategyFactory( build );
    var strategy = factory.Create( Configuration.Release, "custom", null, null, null );
    var versionBefore = await strategy.GetVersionAsync();
    Task.Delay( 1500 ).Wait(); // Ensure wall-clock time advances past the 1-second timestamp resolution
    var versionAfter = await strategy.GetVersionAsync();

    // Assert: _timestamp is cached — both calls return the same version despite the delay
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

    // Assert: rejected eagerly in the factory — prereleaseIdentifiers is meaningless for a Release build
    Assert.Throws<InvalidOperationException>( () =>
      factory.Create( Configuration.Release, "custom", null, null, null )
    );
  }

  [Explicit( "Implement" )]
  [Test]
  public async Task ReleaseValid() {
    // Arrange
    var build = new TestNukeBuild()
      .WithExecutionPlan( b => b.CreateRelease )
      .WithReleaseType( ReleaseType.Release );

    // Act
    var factory = new VersioningStrategyFactory( build );
    var strategy = factory.Create( Configuration.Release, null, null, null, null );

    // Assert
    await Assert.That( async () => await strategy.GetVersionAsync() ).ThrowsNothing();
  }

  public static IEnumerable<Func<NukeBuildWithArbitraryTarget, (NukeBuildWithArbitraryTarget Build, string?
      BuildVersion, string? ExactVersion, Type ExpectedStrategyType)>>
    ReleaseTypePropagatesCases() {
    yield return b => (
      b.WithExecutionPlan( x => x.Arbitrary ).WithReleaseType( ReleaseType.Release ),
      null,
      null,
      typeof(ReleaseVersioning)
    );
    yield return b => (
      b.WithExecutionPlan( x => x.Arbitrary ).WithReleaseType( ReleaseType.PreRelease ),
      "custom",
      null,
      typeof(PreReleaseVersioning)
    );
    yield return b => (
      b.WithExecutionPlan( x => x.Arbitrary ).WithReleaseType( ReleaseType.PreRelease ),
      null,
      "0.0.0-custom.20260319202632",
      typeof(ExactVersioning)
    );
    yield return b => (
      b.WithExecutionPlan( x => x.Arbitrary ).WithReleaseType( ReleaseType.Release ),
      null,
      "1.5.0",
      typeof(ExactVersioning)
    );
  }

  [Test]
  [MethodDataSource( nameof(ReleaseTypePropagatesCases) )]
  public async Task ReleaseTypePropagatesWithoutReleaseTargetInPlan(
    Func<NukeBuildWithArbitraryTarget, (NukeBuildWithArbitraryTarget Build, string? BuildVersion, string? ExactVersion,
      Type ExpectedStrategyType)> caseFactory ) {
    // Arrange — build jobs pass --releasetype but run an arbitrary target, not CreateRelease/CreatePreRelease
    // ReleaseType should still select the matching versioning strategy (for correct artifact naming)
    var (build, buildVersion, exactVersion, expectedStrategyType) = caseFactory( new NukeBuildWithArbitraryTarget() );

    // Act
    var factory = new VersioningStrategyFactory( build );
    var strategy = factory.Create( Configuration.Release, buildVersion, exactVersion, null, null );

    // Assert — strategy matches expected type
    await Assert.That( strategy.GetType() ).IsEqualTo( expectedStrategyType );
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
    // Arrange — construct directly since the factory treats whitespace-only as "not set"
    var strategy = new ExactVersioning( Configuration.Release, "   ", null!, null! );

    // Assert
    Assert.ThrowsAsync<InvalidOperationException>( async () => await strategy.GetVersionAsync() );
  }

  [Test]
  public async Task ExactVersioningValidWithReleaseType() {
    // Arrange — exactVersion is now valid for Release builds too
    var build = new TestNukeBuild()
      .WithExecutionPlan( b => b.CreateRelease )
      .WithReleaseType( ReleaseType.Release );

    // Act
    var factory = new VersioningStrategyFactory( build );
    var strategy = factory.Create( Configuration.Release, null, "1.5.0", null, null );

    // Assert: ExactVersioning is selected regardless of ReleaseType
    await Assert.That( strategy.GetType() ).IsEqualTo( typeof(ExactVersioning) );
  }

  [Test]
  public async Task ExactVersioningValidWithReleaseVersion() {
    // Arrange — a plain release version (non-prerelease) is accepted verbatim
    var build = new TestNukeBuild()
      .WithExecutionPlan( b => b.CreateRelease )
      .WithReleaseType( ReleaseType.Release );

    // Act
    var factory = new VersioningStrategyFactory( build );
    var strategy = factory.Create( Configuration.Release, null, "1.5.0", null, null );
    var version = await strategy.GetVersionAsync();

    // Assert
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