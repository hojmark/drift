using Drift.Cli.Abstractions;
using Drift.Cli.Settings.V1_preview;

namespace Drift.Cli.Tests.Commands;

internal sealed partial class EnvCommandTests {
  [Test]
  public async Task EnvUse_Success_SetsActiveEnvironment() {
    // Arrange
    CreateInitialEnvironments(
      ( "env1", "host1:5000" ),
      ( "env2", "host2:5000" )
    );

    // Act
    var (exitCode, output, error) = await InvokeAsync( "env use env2" );

    // Assert
    await Verify( output.ToString() + error );
    Assert.That( exitCode, Is.EqualTo( ExitCodes.Success ) );

    var settings = ReadSettings();
    Assert.That( settings.ActiveEnvironment, Is.EqualTo( "env2" ) );
  }

  [Test]
  public async Task EnvUse_NonExistentEnvironment_FailsWithError() {
    // Arrange
    CreateInitialEnvironment( "env1", "host1:5000" );

    // Act
    var (exitCode, output, error) = await InvokeAsync( "env use nonexistent" );

    // Assert
    await Verify( output.ToString() + error );
    Assert.That( exitCode, Is.EqualTo( ExitCodes.GeneralError ) );

    var settings = ReadSettings();
    Assert.That( settings.ActiveEnvironment, Is.EqualTo( "env1" ) );
  }

  [Test]
  public async Task EnvUse_NoEnvironments_FailsWithError() {
    // Arrange / Act
    var (exitCode, output, error) = await InvokeAsync( "env use myenv" );

    // Assert
    await Verify( output.ToString() + error );
    Assert.That( exitCode, Is.EqualTo( ExitCodes.GeneralError ) );
  }

  [Test]
  public async Task EnvUse_SwitchBetweenEnvironments_Works() {
    // Arrange
    CreateInitialEnvironments(
      ( "env1", "host1:5000" ),
      ( "env2", "host2:5000" ),
      ( "env3", "host3:5000" )
    );

    var settings = ReadSettings();
    settings.ActiveEnvironment = "env1";
    WriteSettings( settings );

    // Act - switch to env2
    var (exitCode1, _, _) = await InvokeAsync( "env use env2" );
    Assert.That( exitCode1, Is.EqualTo( ExitCodes.Success ) );

    // Act - switch to env3
    var (exitCode2, _, _) = await InvokeAsync( "env use env3" );

    // Assert
    Assert.That( exitCode2, Is.EqualTo( ExitCodes.Success ) );

    var updatedSettings = ReadSettings();
    Assert.That( updatedSettings.ActiveEnvironment, Is.EqualTo( "env3" ) );
  }

  [Test]
  public async Task EnvUse_MissingName_FailsWithError() {
    // Arrange / Act
    var (exitCode, _, error) = await InvokeAsync( "env use" );

    // Assert
    Assert.That( exitCode, Is.EqualTo( ExitCodes.SystemCommandLineDefaultError ) );
    Assert.That( error.ToString(), Does.Contain( "Required argument missing for command: 'use'." ) );
  }
}
