using System.Text.Json;
using Drift.Domain;
using Microsoft.Extensions.Logging;

namespace Drift.Agent.Hosting.Identity;

public partial class AgentIdentity {
  private IAgentIdentityLocationProvider? _loadLocation;

  public static AgentIdentity Load( ILogger? logger = null, IAgentIdentityLocationProvider? location = null ) {
    try {
      location ??= new DefaultAgentIdentityLocationProvider();

      logger?.LogTrace( "Loading agent identity from {Path}", location.GetFile() );

      if ( !File.Exists( location.GetFile() ) ) {
        logger?.LogDebug( "Agent identity file not found. Generating new identity." );
        var newIdentity = CreateNew();
        newIdentity._loadLocation = location;
        return newIdentity;
      }

      var json = File.ReadAllText( location.GetFile() );
      var identity = JsonSerializer.Deserialize<AgentIdentity>( json, AgentIdentityJsonContext.Default.AgentIdentity );

      logger?.LogTrace( "Loaded agent identity: {AgentId}", identity?.Id );

      if ( identity == null ) {
        logger?.LogWarning( "Deserialized identity is null. Generating new identity." );
        var newIdentity = CreateNew();
        newIdentity._loadLocation = location;
        return newIdentity;
      }

      identity._loadLocation = location;

      return identity;
    }
    catch ( Exception e ) {
      logger?.LogError( e, "Error loading agent identity" );
      var newIdentity = CreateNew();
      newIdentity._loadLocation = location;
      return newIdentity;
    }
  }

  public void Save( ILogger logger, IAgentIdentityLocationProvider? location = null ) {
    location ??= new DefaultAgentIdentityLocationProvider();

    logger.LogTrace( "Saving agent identity to {Path}", location.GetFile() );

    if ( !Directory.Exists( location.GetDirectory() ) ) {
      Directory.CreateDirectory( location.GetDirectory() );
    }

    if ( !File.Exists( location.GetFile() ) ) {
      logger.LogInformation( "Creating new agent identity file at {Path}", location.GetFile() );
    }
    else if ( _loadLocation == null ||
              !_loadLocation.GetFile().Equals( location.GetFile(), StringComparison.Ordinal ) // Casing matters on Linux
            ) {
      throw new InvalidOperationException( "Prevented overwriting an existing file, which had not first been loaded." );
    }

    var json = JsonSerializer.Serialize( this, AgentIdentityJsonContext.Default.AgentIdentity );
    File.WriteAllText( location.GetFile(), json );

    // logger.LogDebug( "Agent identity saved: {AgentId}", Id );
  }

  private static AgentIdentity CreateNew() {
    return new AgentIdentity {
      Id = AgentId.New(),
      CreatedAt = DateTime.UtcNow
    };
  }
}
