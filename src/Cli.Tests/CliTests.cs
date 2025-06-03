using System.CommandLine;
using System.CommandLine.IO;
using Drift.Cli.Abstractions;
using Drift.Cli.Commands.Scan;
using Microsoft.Extensions.Logging.Abstractions;

namespace Drift.Cli.Tests;

public class CliTests {
  //TODO fix
  [Explicit]
  [Test]
  public async Task HostTest() {
    // Arrange
    var rootCommand = new RootCommand();
    rootCommand.AddCommand( new ScanCommand( new NullLoggerFactory() ) );
    //TODO rootCommand.AddScanCommand();

    var console = new TestConsole();

    // Act
    var result = await rootCommand.InvokeAsync( "scan", console );

    // Assert
    var outputText = console.Out.ToString().Trim();

    Assert.Multiple( () => {
      Assert.That( outputText, Is.Not.Empty );
      Assert.That( result, Is.EqualTo( ExitCodes.Success ) );
    } );

    await Verify( outputText );
  }
}