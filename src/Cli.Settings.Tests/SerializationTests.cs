using System.Text.Json;
using Drift.Cli.Settings.Serialization;
using Drift.Cli.Settings.V1_preview;
using Drift.Cli.Settings.V1_preview.Environments;
using Drift.Cli.Settings.V1_preview.FeatureFlags;
using Microsoft.Extensions.Logging.Abstractions;

namespace Drift.Cli.Settings.Tests;

internal sealed class SerializationTests {
  [Test]
  public async Task DefaultContents() {
    // Arrange
    ISettingsLocation location = new TemporarySettingsLocation();

    // Act
    new CliSettings().Write( NullLogger.Instance, location );

    // Assert
    var json = await File.ReadAllTextAsync( location.File );
    Console.WriteLine( json );
    await Verify( json );

    Directory.Delete( location.Directory, true );
  }

  [Test]
  public void WriteAndReadRoundtrip() {
    // Arrange
    ISettingsLocation location = new TemporarySettingsLocation();
    var logger = NullLogger.Instance;
    var original = new CliSettings {
      Features = {
        new FeatureFlagSetting( new FeatureFlag( "agent" ), true ),
        new FeatureFlagSetting( new FeatureFlag( "nonexistingfeature" ), false )
      }
    };

    // Act
    original.Write( logger, location );
    var reloaded = CliSettings.Read( location, logger );

    // Assert
    using ( Assert.EnterMultipleScope() ) {
      Assert.That( reloaded.Features, Has.Count.EqualTo( 2 ) );
      Assert.That( reloaded.Features[0].Name.Name, Is.EqualTo( "agent" ) );
      Assert.That( reloaded.Features[0].Enabled, Is.True );
    }

    Directory.Delete( location.Directory, true );
  }

  [Test]
  public void EnvironmentsWriteAndReadRoundtrip() {
    // Arrange
    ISettingsLocation location = new TemporarySettingsLocation();
    var logger = NullLogger.Instance;
    var original = new CliSettings {
      Environments = {
        new EnvironmentSetting( "main-site", "192.168.1.10:51515" ),
        new EnvironmentSetting( "backup-site", "192.168.2.10:51515" )
      },
      ActiveEnvironment = "main-site"
    };

    // Act
    original.Write( logger, location );
    var reloaded = CliSettings.Read( location, logger );

    // Assert
    using ( Assert.EnterMultipleScope() ) {
      Assert.That( reloaded.Environments, Has.Count.EqualTo( 2 ) );
      Assert.That( reloaded.Environments[0].Name, Is.EqualTo( "main-site" ) );
      Assert.That( reloaded.Environments[0].Address, Is.EqualTo( "192.168.1.10:51515" ) );
      Assert.That( reloaded.ActiveEnvironment, Is.EqualTo( "main-site" ) );
      Assert.That( reloaded.GetActiveEnvironment(), Is.EqualTo( original.Environments[0] ) );
    }

    Directory.Delete( location.Directory, true );
  }

  [Test]
  public async Task LoadsDefaultsWhenBadJson() {
    // Arrange
    ISettingsLocation location = new TemporarySettingsLocation();
    Directory.CreateDirectory( location.Directory );
    await File.WriteAllTextAsync( location.File, "garbage" );
    var defaultSettings = new CliSettings();

    // Act
    var loadedSettings = CliSettings.Read( location, NullLogger.Instance );

    // Assert
    var defaultSettingsJson = JsonSerializer.Serialize( defaultSettings );
    var loadedSettingsJson = JsonSerializer.Serialize( loadedSettings );
    Assert.That( defaultSettingsJson, Is.EqualTo( loadedSettingsJson ) );

    Directory.Delete( location.Directory, true );
  }

  [Test]
  public void ReturnsDefaultsWhenNoFile() {
    // Arrange
    ISettingsLocation location = new TemporarySettingsLocation();

    // Act
    var loadedSettings = CliSettings.Read( location, NullLogger.Instance );

    // Assert
    var defaultSettingsJson = JsonSerializer.Serialize( new CliSettings() );
    var loadedSettingsJson = JsonSerializer.Serialize( loadedSettings );
    Assert.That( defaultSettingsJson, Is.EqualTo( loadedSettingsJson ) );
  }

  [Test]
  public void CannotOverwriteWhenNotLoaded() {
    // Arrange
    ISettingsLocation location = new TemporarySettingsLocation();
    new CliSettings().Write( NullLogger.Instance, location );

    // Act / Assert
    Assert.Throws<InvalidOperationException>( () => new CliSettings().Write( NullLogger.Instance, location ) );

    Directory.Delete( location.Directory, true );
  }

  [Test]
  public void CannotOverwriteWhenLoadedFromDifferentFile() {
    // Arrange
    ISettingsLocation location1 = new TemporarySettingsLocation();
    ISettingsLocation location2 = new TemporarySettingsLocation();
    new CliSettings().Write( NullLogger.Instance, location1 );
    new CliSettings().Write( NullLogger.Instance, location2 );
    var reloaded1 = CliSettings.Read( location1, NullLogger.Instance );

    // Act / Assert
    Assert.Throws<InvalidOperationException>( () => reloaded1.Write( NullLogger.Instance, location2 ) );

    Directory.Delete( location1.Directory, true );
    Directory.Delete( location2.Directory, true );
  }

  [Test]
  public async Task CannotOverwriteWhenDefaultsWereReturnedDueToBadJson() {
    // Arrange
    ISettingsLocation location = new TemporarySettingsLocation();
    Directory.CreateDirectory( location.Directory );
    await File.WriteAllTextAsync( location.File, "garbage" );

    // Act
    var loadedSettings = CliSettings.Read( location, NullLogger.Instance );

    // Assert
    Assert.Throws<InvalidOperationException>( () => loadedSettings.Write( NullLogger.Instance, location ) );

    Directory.Delete( location.Directory, true );
  }
}