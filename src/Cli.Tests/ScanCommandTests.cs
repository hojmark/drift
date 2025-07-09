using Drift.Cli.Abstractions;

namespace Drift.Cli.Tests;

public class ScanCommandTests {
  //TODO implement tests for scan command

  [Test]
  public async Task NonExistingSpecOption() {
    var originalOut = Console.Out;
    try {
      // Arrange
      var console = new TestConsole();
      Console.SetOut( console.Out );
      Console.SetError( console.Error );
      var parser = RootCommandFactory.CreateParser();

      // Act
      var result = await parser.InvokeAsync( "scan blah_spec.yaml" );

      // Assert
      using ( Assert.EnterMultipleScope() ) {
        Assert.That( result, Is.EqualTo( ExitCodes.GeneralError ) );
        await Verify( console.Out.ToString() + console.Error );
      }
    }
    finally {
      Console.SetOut( originalOut );
    }
  }
}