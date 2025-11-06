using Drift.Cli.Settings.FeatureFlags;
using Drift.Cli.Settings.Serialization;
using Microsoft.Extensions.Logging.Abstractions;

namespace Drift.Cli.Settings.Tests;

internal sealed class SerializationTests {
  [Test]
  public async Task DefaultSettingsContentsTest() {
    // Arrange
    var location = (ISettingsLocationProvider) new TemporarySettingsLocationProvider();

    // Act
    new CliSettings().Save( NullLogger.Instance, location );

    // Assert
    var json = await File.ReadAllTextAsync( location.GetFile() );
    Console.WriteLine( json );
    await Verify( json );

    Directory.Delete( location.GetDirectory(), true );
  }

  [Test]
  public void SaveAndLoadShouldRoundtripCorrectlyTest() {
    // Arrange
    var location = (ISettingsLocationProvider) new TemporarySettingsLocationProvider();
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
}