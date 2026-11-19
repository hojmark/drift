using System.Text.Json.Serialization;
using Drift.Hosting.Configuration;
using Microsoft.Extensions.Configuration;

namespace Drift.Agent.Host;

/// <summary>
/// Agent host options.
/// </summary>
public sealed class AgentOptions {
  public const ushort DefaultPort = 51515;

  /// <summary>
  /// Gets or sets the port for inbound connections (gRPC).
  /// </summary>
  public ushort Port {
    get;
    set;
  } = DefaultPort;

  /// <summary>
  /// Gets or sets a value indicating whether to listen for inbound server connections on <see cref="Port"/>.
  /// Outbound connections are possible regardless.
  /// </summary>
  public bool Listen {
    get;
    set;
  } = true;

  public RequestLoggingOptions RequestLogging {
    get;
    set;
  } = new() { Enabled = false };

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
  public static AgentOptions Bind( IConfiguration configuration ) {
    var options = new AgentOptions();
    configuration.Bind( options, binder => binder.ErrorOnUnknownConfiguration = true );
    return options;
  }

  internal IReadOnlyCollection<string> Validate() {
    var failures = new List<string>();

    if ( Listen && Port == 0 ) {
      failures.Add( $"{nameof(Port)} must be between 1 and {ushort.MaxValue}." );
    }

    return failures;
  }
}