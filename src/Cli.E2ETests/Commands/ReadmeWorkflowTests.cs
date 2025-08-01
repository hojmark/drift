using System.Text.RegularExpressions;
using Drift.Cli.Abstractions;

namespace Drift.Cli.E2ETests.Commands;

public class ReadmeWorkflowTests : DriftBinaryFixture {
  //TODO implement
  [Explicit( "Relies on a real network scan. Need to create a mock network." )]
  [Test]
  public async Task InitThenScanTest() {
    try {
      var c = new CancellationTokenSource( TimeSpan.FromSeconds( 30 ) );
      var initResult = await DriftBinary
        .ExecuteAsync( "init unittest --discover --overwrite -vv", c.Token );

      TestContext.Out.WriteLine( "STD OUT:\n" + initResult.StdOut );
      TestContext.Out.WriteLine( "ERR OUT:\n" + initResult.ErrOut );

      using ( Assert.EnterMultipleScope() ) {
        Assert.That( initResult.ExitCode, Is.EqualTo( ExitCodes.Success ) );
        Assert.That( initResult.StdOut, Contains.Substring( "✔ Created spec /" ) );
      }

      await Verify( initResult.StdOut )
        .ScrubEmptyLines()
        .ScrubLinesWithReplace( line =>
          Regex.Replace(
            line,
            @"^.+Scanning network\.\.\.$",
            ""
          )
        );

      var scanResult = await DriftBinary.ExecuteAsync( "scan unittest" );

      TestContext.Out.WriteLine( "STD OUT:\n" + initResult.StdOut );
      TestContext.Out.WriteLine( "ERR OUT:\n" + initResult.ErrOut );

      Assert.That( scanResult.ExitCode, Is.EqualTo( ExitCodes.Success ) );

      using ( Assert.EnterMultipleScope() ) {
        Assert.That( scanResult.StdOut, Contains.Substring( "Using network spec" ) );
        Assert.That( scanResult.StdOut, Contains.Substring( "Ping Scan" ) );
        Assert.That( scanResult.StdOut, Contains.Substring( "Indirect ARP Scan" ) );
        Assert.That( scanResult.StdOut, Contains.Substring( "IP" ) );
        Assert.That( scanResult.StdOut, Contains.Substring( "ID" ) );
        Assert.That( scanResult.StdOut, Contains.Substring( "MAC" ) );
      }

      await Verify( scanResult.StdOut )
        .ScrubEmptyLines()
        .ScrubLinesWithReplace( line =>
          Regex.Replace(
            line,
            @"^.+Scanning network\.\.\.$",
            ""
          )
        );
    }
    catch ( Exception ex ) {
      Assert.Fail( ex.Message );
    }
  }
}