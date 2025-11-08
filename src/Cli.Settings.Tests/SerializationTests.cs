using System.Text.Json;
using Drift.Cli.Settings.FeatureFlags;
using Drift.Cli.Settings.Serialization;
using Microsoft.Extensions.Logging.Abstractions;

namespace Drift.Cli.Settings.Tests;

internal sealed class SerializationTests {
  [Test]
  public async Task DefaultSettingsContentsTest() {
    // Arrange
    ISettingsLocationProvider location = new TemporarySettingsLocationProvider();

    // Act
    new CliSettings().Save( NullLogger.Instance, location );

    // Assert
    var json = await File.ReadAllTextAsync( location.GetFile() );
    Console.WriteLine( json );
    await Verify( json );

    Directory.Delete( location.GetDirectory(), true );
  }

  [Test]
  public void SaveAndLoadRoundtripTest() {
    // Arrange
    ISettingsLocationProvider location = new TemporarySettingsLocationProvider();
    var logger = NullLogger.Instance;
    var original = new CliSettings {
      Features = {
        new FeatureFlagSetting( new FeatureFlag( "agent" ), true ),
        new FeatureFlagSetting( new FeatureFlag( "nonexistingfeature" ), false )
      }
    };

    // Act
    original.Save( logger, location );
    var reloaded = CliSettings.Load( logger, location );

    // Assert
    using ( Assert.EnterMultipleScope() ) {
      Assert.That( reloaded.Features, Has.Count.EqualTo( 2 ) );
      Assert.That( reloaded.Features[0].Name.Name, Is.EqualTo( "agent" ) );
      Assert.That( reloaded.Features[0].Enabled, Is.True );
    }

    Directory.Delete( location.GetDirectory(), true );
  }

  [Test]
  public async Task BadJsonLoadsDefaultsTest() {
    // Arrange
    ISettingsLocationProvider location = new TemporarySettingsLocationProvider();
    Directory.CreateDirectory( location.GetDirectory() );
    await File.WriteAllTextAsync( location.GetFile(), "garbage" );
    var defaultSettings = new CliSettings();

    // Act
    var loadedSettings = CliSettings.Load( NullLogger.Instance, location );

    // Assert
    var defaultSettingsJson = JsonSerializer.Serialize( defaultSettings );
    var loadedSettingsJson = JsonSerializer.Serialize( loadedSettings );
    Assert.That( defaultSettingsJson, Is.EqualTo( loadedSettingsJson ) );

    Directory.Delete( location.GetDirectory(), true );
  }

  [Test]
  public void NoFileLoadsDefaultsTest() {
    // Arrange
    ISettingsLocationProvider location = new TemporarySettingsLocationProvider();

    // Act
    var loadedSettings = CliSettings.Load( NullLogger.Instance, location );

    // Assert
    var defaultSettingsJson = JsonSerializer.Serialize( new CliSettings() );
    var loadedSettingsJson = JsonSerializer.Serialize( loadedSettings );
    Assert.That( defaultSettingsJson, Is.EqualTo( loadedSettingsJson ) );
  }

  [Test]
  public void CannotOverwriteFileWhenNotLoaded() {
    // Arrange
    ISettingsLocationProvider location = new TemporarySettingsLocationProvider();
    new CliSettings().Save( NullLogger.Instance, location );

    // Act / Assert
    Assert.Throws<InvalidOperationException>( () => new CliSettings().Save( NullLogger.Instance, location ) );
  }

  [Test]
  public void CannotOverwriteWhenLoadedFromDifferentFile() {
    // Arrange
    ISettingsLocationProvider location1 = new TemporarySettingsLocationProvider();
    ISettingsLocationProvider location2 = new TemporarySettingsLocationProvider();
    new CliSettings().Save( NullLogger.Instance, location1 );
    new CliSettings().Save( NullLogger.Instance, location2 );
    var reloaded1 = CliSettings.Load( NullLogger.Instance, location1 );

    // Act / Assert
    Assert.Throws<InvalidOperationException>( () => reloaded1.Save( NullLogger.Instance, location2 ) );
  }

  [Test]
  public async Task CannotSaveWhenDefaultsWereLoadedDueToBadJson() {
    // Arrange
    ISettingsLocationProvider location = new TemporarySettingsLocationProvider();
    Directory.CreateDirectory( location.GetDirectory() );
    await File.WriteAllTextAsync( location.GetFile(), "garbage" );

    // Act
    var loadedSettings = CliSettings.Load( NullLogger.Instance, location );

    // Assert
    Assert.Throws<InvalidOperationException>( () => loadedSettings.Save( NullLogger.Instance, location ) );

    Directory.Delete( location.GetDirectory(), true );
  }
}