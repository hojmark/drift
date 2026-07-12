using System.CommandLine;
using Drift.Cli.Abstractions;
using Drift.Cli.Commands.Common.Parameters;

namespace Drift.Cli.Commands.Agent.Subcommands.Start;

internal record AgentStartParameters : BaseParameters {
  internal static class Options {
    internal static readonly Option<bool> Daemon = new("--daemon", "-d") {
      Description = "Run the agent as a background daemon"
    };

    internal static readonly Option<ushort> Port = new("--port", "-p") {
      DefaultValueFactory = _ => Ports.AgentDefault, Description = "Set the port used for communication"
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

  public ushort Port {
    get;
    set;
  }

  public string? Id {
    get;
    set;
  }
}