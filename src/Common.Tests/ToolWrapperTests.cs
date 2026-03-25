using System.Runtime.InteropServices;

namespace Drift.Common.Tests;

internal sealed class ToolWrapperTests {
  [Test]
  public async Task StdOutTest() {
    ToolWrapper tool;
    string arguments;

    if ( RuntimeInformation.IsOSPlatform( OSPlatform.Windows ) ) {
      tool = new ToolWrapper( "cmd.exe" );
      arguments = "/c echo Hello world!";
    }
    else {
      tool = new ToolWrapper( "echo" );
      arguments = "Hello world!";
    }

    var result = ( await tool.ExecuteAsync( arguments ) ).StdOut;
    Assert.That( result, Is.EqualTo( $"Hello world!{Environment.NewLine}" ) );
  }
}