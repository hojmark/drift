using System.Threading.Tasks;
using Drift.Build.Utilities.Tests.NukeBuild;
using Drift.Build.Utilities.Versioning;
using Drift.Build.Utilities.Versioning.Strategies;
using Nuke.Common;
using Assert = NUnit.Framework.Assert;

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
    using ( Assert.EnterMultipleScope() ) {
      Assert.That( version.ToString(), Is.EqualTo( "0.0.0-local" ).Or.EqualTo( "0.0.0-ci") );
      Assert.That( strategy.Release, Is.Null );
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
    using ( Assert.EnterMultipleScope() ) {
      Assert.That( version.ToString(), Is.EqualTo( "0.0.0-local" ).Or.EqualTo( "0.0.0-ci") );
      Assert.That( strategy.Release, Is.Null );
    }
  }

  [Test]
  public void MultipleReleaseTargetsThrows() {
    // Arrange
    var build = new TestNukeBuild().WithExecutionPlan( b => b.Release, b => b.PreRelease );

    // Act
    var factory = new VersioningStrategyFactory( build );

    // Assert
    var exception = Assert.Throws<InvalidOperationException>( () =>
      factory.Create( Configuration.Release, null, null, null )
    );
    Assert.That( exception.Message, Is.EqualTo( "Execution plan cannot contain both Release and PreRelease" ) );
  }

  [Test]
  public void LocalReleaseThrows() {
    // Arrange
    var build = new TestNukeBuild().WithExecutionPlan( b => b.PreRelease ).AllowLocalRelease( false );

    // Act
    var factory = new VersioningStrategyFactory( build );

    // Assert
    Assert.Throws<InvalidOperationException>( () => factory.Create( Configuration.Release, null, null, null ) );
  }


  [Test]
  public void PreReleaseWithoutCustomVersionThrows() {
    // Arrange
    var build = new TestNukeBuild().WithExecutionPlan( b => b.PreRelease ).AllowLocalRelease();

    // Act
    var factory = new VersioningStrategyFactory( build );
    var strategy = factory.Create( Configuration.Release, null, null, null );

    // Assert
    Assert.ThrowsAsync<InvalidOperationException>( async () => await strategy.GetVersionAsync() );
  }

  [Test]
  public void PreReleaseWithDebugConfigurationThrows() {
    // Arrange
    var build = new TestNukeBuild().WithExecutionPlan( b => b.PreRelease ).AllowLocalRelease();

    // Act
    var factory = new VersioningStrategyFactory( build );

    // Assert
    Assert.Throws<InvalidOperationException>( () => factory.Create( Configuration.Debug, "0.0.0-custom", null, null ) );
  }

  [Test]
  public void PreReleaseValid() {
    // Arrange
    var build = new TestNukeBuild().WithExecutionPlan( b => b.PreRelease ).AllowLocalRelease();

    // Act
    var factory = new VersioningStrategyFactory( build );
    var strategy = factory.Create( Configuration.Release, "0.0.0-custom", null, null );

    // Assert
    Assert.DoesNotThrowAsync( async () => await strategy.GetVersionAsync() ); //TODO test the version!
  }

  [Test]
  public async Task PreReleaseVersionIsTemporallyConsistent() {
    // Arrange
    var build = new TestNukeBuild().WithExecutionPlan( b => b.PreRelease ).AllowLocalRelease();

    // Act
    var factory = new VersioningStrategyFactory( build );
    var strategy = factory.Create( Configuration.Release, "0.0.0-custom", null, null );
    var versionBefore = await strategy.GetVersionAsync();
    Task.Delay( 1500 ).Wait();
    var versionAfter = await strategy.GetVersionAsync();

    // Assert

    Assert.That( versionBefore, Is.EqualTo( versionAfter ) );
  }

  [Test]
  public void ReleaseWithCustomVersionThrows() {
    // Arrange
    var build = new TestNukeBuild().WithExecutionPlan( b => b.Release ).AllowLocalRelease();

    // Act
    var factory = new VersioningStrategyFactory( build );
    var strategy = factory.Create( Configuration.Release, "0.0.0-custom", null, null );

    // Assert
    Assert.ThrowsAsync<InvalidOperationException>( async () => await strategy.GetVersionAsync() );
  }

  [Test]
  public void ReleaseWithDebugConfigurationThrows() {
    // Arrange
    var build = new TestNukeBuild().WithExecutionPlan( b => b.Release ).AllowLocalRelease();

    // Act
    var factory = new VersioningStrategyFactory( build );

    // Assert
    Assert.Throws<InvalidOperationException>( () => factory.Create( Configuration.Debug, null, null, null ) );
  }

  [Explicit( "Implement" )]
  [Test]
  public void ReleaseValid() {
    // Arrange
    var build = new TestNukeBuild().WithExecutionPlan( b => b.Release ).AllowLocalRelease();

    // Act
    var factory = new VersioningStrategyFactory( build );
    var strategy = factory.Create( Configuration.Release, null, null, null );

    // Assert
    Assert.DoesNotThrowAsync( async () => await strategy.GetVersionAsync() );
  }

  private class NukeBuildWithArbitraryTarget : TestNukeBuild {
    public Target Arbitrary => _ => _
      .Executes( () => {
        }
      );
  }
}