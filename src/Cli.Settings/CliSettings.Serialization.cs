using System.Text.Json;
using System.Text.Json.Serialization;
using Drift.Cli.Settings.FeatureFlags;
using Drift.Cli.Settings.Serialization;
using Microsoft.Extensions.Logging;

namespace Drift.Cli.Settings;

public partial class CliSettings {
  private static readonly JsonSerializerOptions SerializerOptions = new() {
    ReadCommentHandling = JsonCommentHandling.Skip,
    PropertyNameCaseInsensitive = true,
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    WriteIndented = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.Never,
    Converters = { new JsonStringEnumConverter( JsonNamingPolicy.CamelCase ), new FeatureFlagJsonConverter() }
  };

  public static CliSettings Load( ILogger? logger = null, ISettingsLocationProvider? location = null ) {
    try {
      location ??= new DefaultSettingsLocationProvider();

      if ( !File.Exists( location.GetFile() ) ) {
        return new CliSettings();
      }

      var json = File.ReadAllText( location.GetFile() );
      var settings = JsonSerializer.Deserialize<CliSettings>( json, SerializerOptions );

      logger?.LogTrace( "Loaded settings: {Settings}", settings );

      if ( settings == null ) {
        logger?.LogWarning( "Deserialized settings is null, using default" );
        return new CliSettings();
      }

      return settings;
    }
    catch ( Exception e ) {
      logger?.LogError( e, "Error loading settings" );
      return new CliSettings();
    }
  }

  public void Save( ILogger logger, ISettingsLocationProvider? location = null ) {
    location ??= new DefaultSettingsLocationProvider();

    if ( !Directory.Exists( location.GetDirectory() ) ) {
      Directory.CreateDirectory( location.GetDirectory() );
    }

    var json = JsonSerializer.Serialize( this, SerializerOptions );
    File.WriteAllText( location.GetFile(), json );
  }
}