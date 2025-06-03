using Drift.Cli.Abstractions;

namespace Drift.Cli.E2ETests.Commands;

public class GlobalOptionsTests : DriftBinaryFixture {
  [Test]
  public async Task VersionOptionTest() {
    var result = await DriftBinary.ExecuteAsync( "--version" );
    Assert.Multiple( () => {
      Assert.That( result.ExitCode, Is.EqualTo( ExitCodes.Success ) );
      Assert.That( result.StdOut, Is.Not.Empty );
      Assert.That( result.ErrOut, Is.Empty );
    } );
  }

  [Test]
  public async Task HelpOptionTest() {
    var resultQuestionMark = await DriftBinary.ExecuteAsync( "-?" );
    var resultH = await DriftBinary.ExecuteAsync( "-h" );
    var resultHelp = await DriftBinary.ExecuteAsync( "--help" );

    // Exit code
    Assert.Multiple( () => {
      Assert.That( resultQuestionMark.ExitCode, Is.EqualTo( ExitCodes.Success ) );
      Assert.That( resultH.ExitCode, Is.EqualTo( ExitCodes.Success ) );
      Assert.That( resultHelp.ExitCode, Is.EqualTo( ExitCodes.Success ) );
    } );

    Assert.Multiple( () => {
      // StdOut
      Assert.That( resultQuestionMark.StdOut, Is.Not.Empty );
      Assert.That( resultH.StdOut, Is.Not.Empty );
      Assert.That( resultHelp.StdOut, Is.Not.Empty );

      // ErrorOut
      Assert.That( resultQuestionMark.ErrOut, Is.Empty );
      Assert.That( resultH.ErrOut, Is.Empty );
      Assert.That( resultHelp.ErrOut, Is.Empty );
    } );
  }
}