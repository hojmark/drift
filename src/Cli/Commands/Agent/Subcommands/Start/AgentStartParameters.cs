using System.CommandLine;
using Drift.Agent.Host;
using Drift.Cli.Abstractions;
using Drift.Cli.Commands.Common.Parameters;

namespace Drift.Cli.Commands.Agent.Subcommands.Start;

internal record AgentStartParameters : BaseParameters {
  internal static class Options {
    internal static readonly Option<bool> Daemon = new("--daemon", "-d") {
      Description = "Run the agent as a background daemon"
    };

    // No default value: an option that is not given must not override the configuration file or environment
    internal static readonly Option<ushort?> Port = new("--port", "-p") {
      Description =
        $"Set the server port (server-to-agent communication). Must match the port exposed by the server. [default: {AgentOptions.DefaultPort}]"
    };

    // TODO remove after hub-and-spoke transition?
    internal static readonly Option<string> Id = new("--id") {
      Description = "Set a specific agent ID (for testing purposes)", Hidden = true
    };
  }

  internal AgentStartParameters( ParseResult parseResult ) : base( parseResult ) {
    Port = parseResult.GetValue( Options.Port );
    Id = parseResult.GetValue( Options.Id );
  }

  public ushort? Port {
    get;
    set;
  }

  public string? Id {
    get;
    set;
  }

  /// <summary>
  /// Applies options given explicitly on the command line. These take precedence over all other sources.
  /// </summary>
  internal void ApplyTo( AgentOptions options ) {
    if ( Port is { } port ) {
      options.Port = port;
    }
  }
}