using Drift.Cli.Abstractions;

namespace Drift.Cli.E2ETests.Binary.Commands;

internal sealed class UnsupportedOptionsTests : DriftBinaryFixture {
  [TestCase( "" )]
  [TestCase( "status" )]
  [TestCase( "env list" )]
  // TODO investigate below cases
  // [TestCase( "--version" )]
  // [TestCase( "agent start --bogus" )]
  // [TestCase( "agent --bogus" )]
  public async Task UnsupportedOption_ReturnsErrorExitCode( string command ) {
    var result = await DriftBinary.ExecuteAsync( command + " --bogus" );

    using ( Assert.EnterMultipleScope() ) {
      Assert.That( result.ExitCode, Is.EqualTo( ExitCodes.SystemCommandLineDefaultError ) );
      Assert.That( result.ErrOut, Does.Contain( "Unrecognized command or argument '--bogus'" ) );
    }
  }
}