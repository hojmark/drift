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
    var strategy = factory.Create( Configuration.Debug, null, null, null );
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
      factory.Create( Configuration.Release, null, null, null )
    );

    // Assert
    await Assert.That( exception.Message )
      .IsEqualTo( "Execution plan cannot contain both CreateRelease and CreatePreRelease" );
  }

  public static IEnumerable<Func<TestNukeBuild, (TestNukeBuild Build, string ExpectedMessageContains)>>
    MismatchedReleaseTypeCases() {
    yield return b => (
      b.WithExecutionPlan( x => x.CreateRelease ).WithReleaseType( ReleaseType.None ),
      nameof(ReleaseType.Release)
    );
    yield return b => (
      b.WithExecutionPlan( x => x.CreatePreRelease ).WithReleaseType( ReleaseType.None ),
      nameof(ReleaseType.PreRelease)
    );
    yield return b => (
      b.WithExecutionPlan( x => x.CreateRelease ).WithReleaseType( ReleaseType.PreRelease ),
      nameof(ReleaseType.Release)
    );
    yield return b => (
      b.WithExecutionPlan( x => x.CreatePreRelease ).WithReleaseType( ReleaseType.Release ),
      nameof(ReleaseType.PreRelease)
    );
  }

  [Test]
  [MethodDataSource( nameof(MismatchedReleaseTypeCases) )]
  public async Task MismatchedReleaseTypeThrows(
    Func<TestNukeBuild, (TestNukeBuild Build, string ExpectedMessageContains)> caseFactory ) {
    // Arrange
    var (build, expectedMessageContains) = caseFactory( new TestNukeBuild() );

    // Act
    var factory = new VersioningStrategyFactory( build );
    var exception = Assert.Throws<InvalidOperationException>( () =>
      factory.Create( Configuration.Release, null, null, null )
    );

    // Assert
    await Assert.That( exception.Message ).Contains( expectedMessageContains );
  }

  [Test]
  public void PreReleaseWithoutCustomVersionThrows() {
    // Arrange
    var build = new TestNukeBuild()
      .WithExecutionPlan( b => b.CreatePreRelease )
      .WithReleaseType( ReleaseType.PreRelease );

    // Act
    var factory = new VersioningStrategyFactory( build );
    var strategy = factory.Create( Configuration.Release, null, null, null );

    // Assert
    Assert.ThrowsAsync<InvalidOperationException>( async () => await strategy.GetVersionAsync() );
  }

  public static IEnumerable<Func<TestNukeBuild, (TestNukeBuild Build, string? CustomVersion)>>
    DebugConfigurationCases() {
    yield return b => (
      b.WithExecutionPlan( x => x.CreatePreRelease ).WithReleaseType( ReleaseType.PreRelease ),
      "0.0.0-custom"
    );
    yield return b => (
      b.WithExecutionPlan( x => x.CreateRelease ).WithReleaseType( ReleaseType.Release ),
      null
    );
  }

  [Test]
  [MethodDataSource( nameof(DebugConfigurationCases) )]
  public void DebugConfigurationThrows(
    Func<TestNukeBuild, (TestNukeBuild Build, string? CustomVersion)> caseFactory ) {
    // Arrange
    var (build, customVersion) = caseFactory( new TestNukeBuild() );

    // Act
    var factory = new VersioningStrategyFactory( build );

    // Assert
    Assert.Throws<InvalidOperationException>( () => factory.Create( Configuration.Debug, customVersion, null, null ) );
  }

  [Test]
  public async Task PreReleaseValid() {
    // Arrange
    var build = new TestNukeBuild()
      .WithExecutionPlan( b => b.CreatePreRelease )
      .WithReleaseType( ReleaseType.PreRelease );

    // Act
    var factory = new VersioningStrategyFactory( build );
    var strategy = factory.Create( Configuration.Release, "0.0.0-custom", null, null );

    // Assert
    await Assert.That( async () => await strategy.GetVersionAsync() ).ThrowsNothing(); // TODO test the version!
  }

  [Test]
  public async Task PreReleaseVersionIsTemporallyConsistent() {
    // Arrange
    var build = new TestNukeBuild()
      .WithExecutionPlan( b => b.CreatePreRelease )
      .WithReleaseType( ReleaseType.PreRelease );

    // Act
    var factory = new VersioningStrategyFactory( build );
    var strategy = factory.Create( Configuration.Release, "0.0.0-custom", null, null );
    var versionBefore = await strategy.GetVersionAsync();
    Task.Delay( 1500 ).Wait();
    var versionAfter = await strategy.GetVersionAsync();

    // Assert
    await Assert.That( versionBefore ).IsEqualTo( versionAfter );
  }

  [Test]
  public void ReleaseWithCustomVersionThrows() {
    // Arrange
    var build = new TestNukeBuild()
      .WithExecutionPlan( b => b.CreateRelease )
      .WithReleaseType( ReleaseType.Release );

    // Act
    var factory = new VersioningStrategyFactory( build );
    var strategy = factory.Create( Configuration.Release, "0.0.0-custom", null, null );

    // Assert
    Assert.ThrowsAsync<InvalidOperationException>( async () => await strategy.GetVersionAsync() );
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
    var strategy = factory.Create( Configuration.Release, null, null, null );

    // Assert
    await Assert.That( async () => await strategy.GetVersionAsync() ).ThrowsNothing();
  }

  public static IEnumerable<Func<NukeBuildWithArbitraryTarget, (NukeBuildWithArbitraryTarget Build, string?
      CustomVersion, Type ExpectedStrategyType)>>
    ReleaseTypePropagatesCases() {
    yield return b => (
      b.WithExecutionPlan( x => x.Arbitrary ).WithReleaseType( ReleaseType.Release ),
      null,
      typeof(ReleaseVersioning)
    );
    yield return b => (
      b.WithExecutionPlan( x => x.Arbitrary ).WithReleaseType( ReleaseType.PreRelease ),
      "0.0.0-custom",
      typeof(PreReleaseVersioning)
    );
  }

  [Test]
  [MethodDataSource( nameof(ReleaseTypePropagatesCases) )]
  public async Task ReleaseTypePropagatesWithoutReleaseTargetInPlan(
    Func<NukeBuildWithArbitraryTarget, (NukeBuildWithArbitraryTarget Build, string? CustomVersion, Type
      ExpectedStrategyType)> caseFactory ) {
    // Arrange — build jobs pass --releasetype but run an arbitrary target, not CreateRelease/CreatePreRelease
    // ReleaseType should still select the matching versioning strategy (for correct artifact naming)
    var (build, customVersion, expectedStrategyType) = caseFactory( new NukeBuildWithArbitraryTarget() );

    // Act
    var factory = new VersioningStrategyFactory( build );
    var strategy = factory.Create( Configuration.Release, customVersion, null, null );

    // Assert — strategy matches ReleaseType, not DefaultVersioning
    await Assert.That( strategy.GetType() ).IsEqualTo( expectedStrategyType );
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