using Drift.Cli.Abstractions;

namespace Drift.Cli.Tests.Commands;

internal sealed partial class EnvCommandTests {
  [Test]
  public async Task EnvList_NoEnvironments_DisplaysEmptyMessage() {
    // Arrange / Act
    var (exitCode, output, error) = await InvokeAsync( "env list" );

    // Assert
    await Verify( output.ToString() + error );
    Assert.That( exitCode, Is.EqualTo( ExitCodes.Success ) );
  }

  [Test]
  public async Task EnvList_SingleEnvironment_DisplaysWithActive( [Values( "list", "ls" )] string commandName ) {
    // Arrange
    CreateInitialEnvironment( "myenv", "localhost:5000" );

    // Act
    var (exitCode, output, error) = await InvokeAsync( $"env {commandName}" );

    // Assert
    await Verify( output.ToString() + error );
    Assert.That( exitCode, Is.EqualTo( ExitCodes.Success ) );
  }

  [Test]
  public async Task EnvList_MultipleEnvironmentsWithOneActive_DisplaysCorrectly() {
    // Arrange
    CreateInitialEnvironments(
      ( "env1", "host1:5000" ),
      ( "env2", "host2:5000" ),
      ( "env3", "host3:5000" )
    );

    var settings = ReadSettings();
    settings.ActiveEnvironment = "env2";
    WriteSettings( settings );

    // Act
    var (exitCode, output, error) = await InvokeAsync( "env list" );

    // Assert
    await Verify( output.ToString() + error );
    Assert.That( exitCode, Is.EqualTo( ExitCodes.Success ) );
  }

  [Test]
  public async Task EnvList_NoActiveEnvironment_DisplaysWarning() {
    // Arrange
    CreateInitialEnvironments(
      ( "env1", "host1:5000" ),
      ( "env2", "host2:5000" )
    );

    var settings = ReadSettings();
    settings.ActiveEnvironment = null;
    WriteSettings( settings );

    // Act
    var (exitCode, output, error) = await InvokeAsync( "env list" );

    // Assert
    await Verify( output.ToString() + error );
    Assert.That( exitCode, Is.EqualTo( ExitCodes.Success ) );
  }
}
