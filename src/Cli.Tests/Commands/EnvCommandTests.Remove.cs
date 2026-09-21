using Drift.Cli.Abstractions;

namespace Drift.Cli.Tests.Commands;

internal sealed partial class EnvCommandTests {
  [Test]
  public async Task EnvRemove_Success_RemovesEnvironment() {
    // Arrange
    CreateInitialEnvironments(
      ( "env1", "host1:5000" ),
      ( "env2", "host2:5000" )
    );

    // Act
    var (exitCode, output, error) = await InvokeAsync( "env remove env1" );

    // Assert
    await Verify( output.ToString() + error );
    Assert.That( exitCode, Is.EqualTo( ExitCodes.Success ) );

    var settings = ReadSettings();
    Assert.That( settings.Environments, Has.Count.EqualTo( 1 ) );
    Assert.That( settings.Environments[0].Name, Is.EqualTo( "env2" ) );
  }

  [Test]
  public async Task EnvRemove_RemovesActiveEnvironment_ClearsActive() {
    // Arrange
    CreateInitialEnvironments(
      ( "env1", "host1:5000" ),
      ( "env2", "host2:5000" )
    );

    var settings = ReadSettings();
    settings.ActiveEnvironment = "env1";
    WriteSettings( settings );

    // Act
    var (exitCode, output, error) = await InvokeAsync( "env remove env1" );

    // Assert
    await Verify( output.ToString() + error );
    Assert.That( exitCode, Is.EqualTo( ExitCodes.Success ) );

    var updatedSettings = ReadSettings();
    Assert.That( updatedSettings.ActiveEnvironment, Is.Null );
    Assert.That( updatedSettings.Environments, Has.Count.EqualTo( 1 ) );
  }

  [Test]
  public async Task EnvRemove_RemovesNonActiveEnvironment_KeepsActiveUnchanged() {
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
    var (exitCode, _, _) = await InvokeAsync( "env remove env1" );

    // Assert
    Assert.That( exitCode, Is.EqualTo( ExitCodes.Success ) );

    var updatedSettings = ReadSettings();
    Assert.That( updatedSettings.ActiveEnvironment, Is.EqualTo( "env2" ) );
    Assert.That( updatedSettings.Environments, Has.Count.EqualTo( 2 ) );
  }

  [Test]
  public async Task EnvRemove_RemoveLastEnvironment_ClearsActive_AdvisesCreatingOne() {
    // Arrange
    CreateInitialEnvironment( "env1", "host1:5000" );

    // Act
    var (exitCode, output, error) = await InvokeAsync( "env remove env1" );

    // Assert
    await Verify( output.ToString() + error );
    Assert.That( exitCode, Is.EqualTo( ExitCodes.Success ) );

    var settings = ReadSettings();
    Assert.That( settings.Environments, Is.Empty );
    Assert.That( settings.ActiveEnvironment, Is.Null );
  }

  [Test]
  public async Task EnvRemove_NonExistentEnvironment_FailsWithError() {
    // Arrange
    CreateInitialEnvironment( "env1", "host1:5000" );

    // Act
    var (exitCode, output, error) = await InvokeAsync( "env remove nonexistent" );

    // Assert
    await Verify( output.ToString() + error );
    Assert.That( exitCode, Is.EqualTo( ExitCodes.GeneralError ) );

    var settings = ReadSettings();
    Assert.That( settings.Environments, Has.Count.EqualTo( 1 ) );
  }

  [Test]
  public async Task EnvRemove_NoEnvironments_FailsWithError() {
    // Arrange / Act
    var (exitCode, output, error) = await InvokeAsync( "env remove myenv" );

    // Assert
    await Verify( output.ToString() + error );
    Assert.That( exitCode, Is.EqualTo( ExitCodes.GeneralError ) );
  }

  [Test]
  public async Task EnvRemove_MissingName_FailsWithError() {
    // Arrange / Act
    var (exitCode, _, error) = await InvokeAsync( "env remove" );

    // Assert
    Assert.That( exitCode, Is.EqualTo( ExitCodes.SystemCommandLineDefaultError ) );
    Assert.That( error.ToString(), Does.Contain( "Required argument missing for command: 'remove'." ) );
  }
}
