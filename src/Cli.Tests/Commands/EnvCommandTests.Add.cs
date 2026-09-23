using Drift.Cli.Abstractions;
using Drift.Cli.Settings.V1_preview;

namespace Drift.Cli.Tests.Commands;

internal sealed partial class EnvCommandTests {
  [Test]
  public async Task EnvAdd_Success_AddsEnvironmentAndSetsActive() {
    // Arrange / Act
    WriteSettings( new CliSettings() );

    var (exitCode, output, error) = await InvokeAsync(
      "env add myenv localhost:5000"
    );

    // Assert
    await Verify( output.ToString() + error );
    Assert.That( exitCode, Is.EqualTo( ExitCodes.Success ) );

    var settings = ReadSettings();
    Assert.That( settings.Environments, Has.Count.EqualTo( 1 ) );
    Assert.That( settings.Environments[0].Name, Is.EqualTo( "myenv" ) );
    Assert.That( settings.Environments[0].Address, Is.EqualTo( "localhost:5000" ) );
    Assert.That( settings.ActiveEnvironment, Is.EqualTo( "myenv" ) );
  }

  [Test]
  public async Task EnvAdd_MultipleEnvironments_OnlyFirstBecomesActive() {
    // Arrange
    CreateInitialEnvironment( "env1", "host1:5000" );

    // Act
    var (exitCode, output, error) = await InvokeAsync( "env add env2 host2:5000" );

    // Assert
    await Verify( output.ToString() + error );
    Assert.That( exitCode, Is.EqualTo( ExitCodes.Success ) );

    var settings = ReadSettings();
    Assert.That( settings.Environments, Has.Count.EqualTo( 2 ) );
    Assert.That( settings.ActiveEnvironment, Is.EqualTo( "env1" ) );
  }

  [Test]
  public async Task EnvAdd_DuplicateName_FailsWithError() {
    // Arrange
    CreateInitialEnvironment( "myenv", "localhost:5000" );

    // Act
    var (exitCode, output, error) = await InvokeAsync(
      "env add myenv localhost:5001"
    );

    // Assert
    await Verify( output.ToString() + error );
    Assert.That( exitCode, Is.EqualTo( ExitCodes.GeneralError ) );

    var settings = ReadSettings();
    Assert.That( settings.Environments, Has.Count.EqualTo( 1 ) );
    Assert.That( settings.Environments[0].Address, Is.EqualTo( "localhost:5000" ) );
  }

  [Test]
  public async Task EnvAdd_MissingName_FailsWithError() {
    // Arrange / Act
    var (exitCode, _, error) = await InvokeAsync( "env add localhost:5000" );

    // Assert
    Assert.That( exitCode, Is.EqualTo( ExitCodes.SystemCommandLineDefaultError ) );
    Assert.That( error.ToString(), Does.Contain( "Required argument missing for command: 'add'." ) );
  }

  [Test]
  public async Task EnvAdd_MissingUri_FailsWithError() {
    // Arrange / Act
    var (exitCode, _, error) = await InvokeAsync( "env add myenv" );

    // Assert
    Assert.That( exitCode, Is.EqualTo( ExitCodes.SystemCommandLineDefaultError ) );
    Assert.That( error.ToString(), Does.Contain( "Required argument missing for command: 'add'." ) );
  }
}
