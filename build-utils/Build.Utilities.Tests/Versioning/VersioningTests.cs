using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Drift.Build.Utilities.Versioning;
using Drift.Build.Utilities.Versioning.Abstractions;
using Drift.Build.Utilities.Versioning.Strategies;
using Nuke.Common;
using Nuke.Common.Execution;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;

namespace Drift.Build.Utilities.Tests.Versioning;

internal sealed class VersioningTests {
  [Test]
  public async Task DefaultVersioningVersionTest() {
    // Arrange
    var testProject = new TestNukeBuild();
    var strategy = new DefaultVersioning( testProject );

    // Act / Assert
    var version = await strategy.GetVersionAsync();

    using ( Assert.EnterMultipleScope() ) {
      Assert.That( version.ToString(), Is.EqualTo( "0.0.0-local" ) );
      Assert.That( strategy.Release, Is.Null );
    }
  }

  [Test]
  public async Task DefaultSupportTest() {
    // Arrange
    var testProject = new TestNukeBuild();
    var strategy = new DefaultVersioning( testProject );

    // Act / Assert
    using ( Assert.EnterMultipleScope() ) {
      Assert.That( strategy.SupportsTarget( testProject.Arbitrary ), Is.True );
      Assert.That( strategy.SupportsTarget( testProject.Release ), Is.False );
      Assert.That( strategy.SupportsTarget( testProject.PreRelease ), Is.False );
    }
  }

  private static TestNukeBuild SetExecutionPlan( TestNukeBuild build, params Target[] targets ) {
    build.ExecutionPlan = targets.Select( target => new ExecutableTarget { Factory = target } ).ToList();
    return build;
  }
}

internal sealed class TestNukeBuild : Nuke.Common.NukeBuild, INukeRelease {
  internal TestNukeBuild() {
    ExecutionPlan = new List<ExecutableTarget>();
  }

  public bool AllowLocalRelease => false;

  public Target Release => _ => _
    .Executes( () => {
      }
    );

  public Target PreRelease => _ => _
    .Executes( () => {
      }
    );

  public Target Arbitrary => _ => _
    .Executes( () => {
      }
    );
}