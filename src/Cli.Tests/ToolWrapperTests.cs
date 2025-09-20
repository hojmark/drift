using Drift.Utils;

namespace Drift.Cli.Tests;

internal sealed class ToolWrapperTests {
  [Test]
  public async Task StdOutTest() {
    var echo = new ToolWrapper( "echo" );
    var result = ( await echo.ExecuteAsync( "Hello world!" ) ).StdOut;
    Assert.That( result, Is.EqualTo( "Hello world!\n" ) );
  }
}