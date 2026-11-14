using System.Text.Json.Serialization;
using Drift.Hosting.Configuration;
using Microsoft.Extensions.Configuration;

namespace Drift.Coordinator.Host;

/// <summary>
/// Server (coordinator) host options. Bound from <c>server.json</c> and <c>DRIFT_SERVER_*</c> environment variables.
/// </summary>
public sealed class CoordinatorOptions {
  public const ushort DefaultPort = 51510;
  public const ushort DefaultAgentPort = 51515;

  /// <summary>
  /// Gets or sets the client port (HTTP control API and UI).
  /// </summary>
  public ushort Port {
    get;
    set;
  } = DefaultPort;

  /// <summary>
  /// Gets or sets the port for inbound agent connections (gRPC).
  /// </summary>
  public ushort AgentPort {
    get;
    set;
  } = DefaultAgentPort;

  /// <summary>
  /// Gets or sets a value indicating whether to listen for inbound agent connections on <see cref="AgentPort"/>.
  /// Outbound connections are possible regardless.
  /// </summary>
  public bool ListenForAgents {
    get;
    set;
  } = true;

  public RequestLoggingOptions RequestLogging {
    get;
    set;
  } = new() { Enabled = true };

  /// <summary>
  /// Gets or sets the JSON schema reference. Allowed in the file for editor support; ignored otherwise.
  /// </summary>
  [ConfigurationKeyName( ConfigurationKeys.JsonSchema )]
  [JsonIgnore]
  public string? Schema {
    get;
    set;
  }

  /// <summary>
  /// Binds options from <paramref name="configuration"/> on top of the defaults.
  /// </summary>
  /// <exception cref="InvalidOperationException">Configuration contains unknown keys or invalid values.</exception>
  public static CoordinatorOptions Bind( IConfiguration configuration ) {
    var options = new CoordinatorOptions();
    configuration.Bind( options, binder => binder.ErrorOnUnknownConfiguration = true );
    return options;
  }

  internal IReadOnlyCollection<string> Validate() {
    var failures = new List<string>();

    if ( Port == 0 ) {
      failures.Add( $"{nameof(Port)} must be between 1 and {ushort.MaxValue}." );
    }

    if ( ListenForAgents && AgentPort == 0 ) {
      failures.Add( $"{nameof(AgentPort)} must be between 1 and {ushort.MaxValue}." );
    }

    if ( ListenForAgents && Port == AgentPort ) {
      failures.Add( $"{nameof(Port)} and {nameof(AgentPort)} must differ (both are {Port})." );
    }

    return failures;
  }
}