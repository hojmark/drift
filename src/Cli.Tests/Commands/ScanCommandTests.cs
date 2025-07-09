using Drift.Cli.Abstractions;

namespace Drift.Cli.Tests.Commands;

public class ScanCommandTests {
  //TODO implement tests for scan command

  [Explicit]
  [Test]
  public async Task HostTest() {
    // Arrange
    var config = TestCommandLineConfiguration.Create();

    // Act
    var result = await config.InvokeAsync( "scan blah_spec.yaml" );

    // Assert
    using ( Assert.EnterMultipleScope() ) {
      await Verify( config.Output.ToString() + config.Error );
      Assert.That( result, Is.EqualTo( ExitCodes.Success ) );
    }
  }

  [Test]
  public async Task NonExistingSpecOption() {
    // Arrange
    var config = TestCommandLineConfiguration.Create();

    // Act
    var result = await config.InvokeAsync( "scan blah_spec.yaml" );

    // Assert
    using ( Assert.EnterMultipleScope() ) {
      Assert.That( result, Is.EqualTo( ExitCodes.GeneralError ) );
      await Verify( config.Output.ToString() + config.Error );
    }
  }
}