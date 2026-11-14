using System.Text.Json;
using Drift.Common.IO;
using Microsoft.Extensions.Logging;

namespace Drift.Cli.Settings.V1_preview;

public partial class CliSettings {
  private IDriftSettingsLocation? _loadLocation;

  public static CliSettings Read( IDriftSettingsLocation location, ILogger? logger = null ) {
    try {
      logger?.LogTrace( "Reading settings from file: {Path}", location.File );

      if ( !File.Exists( location.File ) ) {
        logger?.LogDebug( "Settings file not found. Using defaults." );
        return new CliSettings();
      }

      var json = File.ReadAllText( location.File );
      var settings = JsonSerializer.Deserialize<CliSettings>( json, CliSettingsJsonContext.Default.CliSettings );

      logger?.LogTrace( "Settings: {Settings}", settings );

      if ( settings == null ) {
        logger?.LogWarning( "Deserialized settings is null. Using defaults." );
        return new CliSettings();
      }

      settings._loadLocation = location;

      return settings;
    }
    catch ( Exception e ) {
      logger?.LogError( e, "Error reading settings file: {Path}. Using defaults.", location.File );
      return new CliSettings();
    }
  }

  public void Write( ILogger logger, IDriftSettingsLocation location ) {
    logger.LogTrace( "Writing settings to file: {Path}", location.File );

    if ( !Directory.Exists( location.Directory ) ) {
      Directory.CreateDirectory( location.Directory );
    }

    if ( !File.Exists( location.File ) ) {
      logger.LogInformation( "Creating new settings file: {Path}", location.File );
    }
    else if ( _loadLocation == null ||
              !_loadLocation.File.Equals( location.File, StringComparison.Ordinal ) // Casing matters on Linux
            ) {
      throw new InvalidOperationException( "Prevented overwriting an existing file, which had not first been loaded." );
    }

    var json = JsonSerializer.Serialize( this, CliSettingsJsonContext.Default.CliSettings );
    File.WriteAllText( location.File, json );
  }
}