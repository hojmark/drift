using System.Text.Json;
using Drift.Cli.Settings.Serialization;
using Microsoft.Extensions.Logging;

namespace Drift.Cli.Settings.V1_preview;

public partial class CliSettings {
  private ISettingsLocationProvider? _loadLocation;

  public static CliSettings Load( ILogger? logger = null, ISettingsLocationProvider? location = null ) {
    try {
      location ??= new DefaultSettingsLocationProvider();

      logger?.LogTrace( "Loading settings from {Path}", location.GetFile() );

      if ( !File.Exists( location.GetFile() ) ) {
        logger?.LogInformation( "Settings file not found. Using default." );
        return new CliSettings();
      }

      var json = File.ReadAllText( location.GetFile() );
      var settings = JsonSerializer.Deserialize<CliSettings>( json, CliSettingsJsonContext.Default.CliSettings );

      logger?.LogTrace( "Loaded settings: {Settings}", settings );

      if ( settings == null ) {
        logger?.LogWarning( "Deserialized settings is null. Using default." );
        return new CliSettings();
      }

      settings._loadLocation = location;

      return settings;
    }
    catch ( Exception e ) {
      logger?.LogError( e, "Error loading settings" );
      return new CliSettings();
    }
  }

  public void Save( ILogger logger, ISettingsLocationProvider? location = null ) {
    location ??= new DefaultSettingsLocationProvider();

    logger.LogTrace( "Saving settings to {Path}", location.GetFile() );

    if ( !Directory.Exists( location.GetDirectory() ) ) {
      Directory.CreateDirectory( location.GetDirectory() );
    }

    if ( !File.Exists( location.GetFile() ) ) {
      logger.LogInformation( "Creating new settings file at {Path}", location.GetFile() );
    }
    else if ( _loadLocation == null ||
              !_loadLocation.GetFile().Equals( location.GetFile(), StringComparison.Ordinal ) // Casing matters on Linux
            ) {
      throw new InvalidOperationException( "Prevented overwriting an existing file, which had not first been loaded." );
    }

    var json = JsonSerializer.Serialize( this, CliSettingsJsonContext.Default.CliSettings );
    File.WriteAllText( location.GetFile(), json );
  }
}