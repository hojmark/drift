using Drift.Cli.Abstractions;

namespace Drift.Cli.E2ETests.Binary.Commands;

internal sealed class LintTests : DriftBinaryFixture {
  private const string SpecName = "unittest";
  private const string SpecFileName = $"{SpecName}.spec.yaml";

  [Test]
  public async Task InitThenLintTest() {
    try {
      // Clean up any leftover spec file from a previous run
      File.Delete( SpecFileName );

      // Arrange
      var initResult = await DriftBinary.ExecuteAsync( $"init {SpecName}" );

      TestContext.Out.WriteLine( "STD OUT:\n" + initResult.StdOut );
      TestContext.Out.WriteLine( "ERR OUT:\n" + initResult.ErrOut );

      using ( Assert.EnterMultipleScope() ) {
        Assert.That( initResult.ExitCode, Is.EqualTo( ExitCodes.Success ) );
        Assert.That( initResult.StdOut, Contains.Substring( " Spec created" ) );
        Assert.That( initResult.ErrOut, Is.Empty );
      }

      // Act
      var lintResult = await DriftBinary.ExecuteAsync( $"lint {SpecName}" );

      TestContext.Out.WriteLine( "STD OUT:\n" + initResult.StdOut );
      TestContext.Out.WriteLine( "ERR OUT:\n" + initResult.ErrOut );

      // Assert
      using ( Assert.EnterMultipleScope() ) {
        Assert.That( lintResult.ExitCode, Is.EqualTo( ExitCodes.Success ) );
        await Verify( lintResult.StdOut );
        Assert.That( lintResult.ErrOut, Is.Empty );
      }
    }
    finally {
      File.Delete( SpecFileName );
    }
  }
}