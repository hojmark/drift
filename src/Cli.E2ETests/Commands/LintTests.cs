﻿using Drift.Cli.Abstractions;

namespace Drift.Cli.E2ETests.Commands;

public class LintTests : DriftBinaryFixture {
  [Test]
  public async Task InitThenLintTest() {
    try {
      // Arrange
      var initResult = await DriftBinary.ExecuteAsync( "init unittest" );

      TestContext.Out.WriteLine( "STD OUT:\n" + initResult.StdOut );
      TestContext.Out.WriteLine( "ERR OUT:\n" + initResult.ErrOut );

      using ( Assert.EnterMultipleScope() ) {
        Assert.That( initResult.ExitCode, Is.EqualTo( ExitCodes.Success ) );
        Assert.That( initResult.StdOut, Contains.Substring( "✅️ Created spec: " ) );
        Assert.That( initResult.ErrOut, Is.Empty );
      }

      // Act
      var lintResult = await DriftBinary.ExecuteAsync( "lint unittest" );

      TestContext.Out.WriteLine( "STD OUT:\n" + initResult.StdOut );
      TestContext.Out.WriteLine( "ERR OUT:\n" + initResult.ErrOut );

      // Assert
      using ( Assert.EnterMultipleScope() ) {
        Assert.That( lintResult.ExitCode, Is.EqualTo( ExitCodes.Success ) );
        Assert.That( lintResult.StdOut, Contains.Substring( "✅️ Spec is valid" ) );
        Assert.That( lintResult.ErrOut, Is.Empty );
        await Verify( lintResult.StdOut );
      }
    }
    catch ( Exception ex ) {
      Assert.Fail( ex.Message );
    }
  }
}