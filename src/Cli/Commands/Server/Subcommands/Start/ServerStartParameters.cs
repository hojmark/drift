using System.CommandLine;
using Drift.Cli.Abstractions;
using Drift.Cli.Commands.Common.Parameters;

namespace Drift.Cli.Commands.Server.Subcommands.Start;

internal record ServerStartParameters : BaseParameters {
  internal static class Options {
    // TODO implement
    internal static readonly Option<bool> Daemon = new("--daemon", "-d") {
      Description = "Run the server as a background daemon"
    };

    // TODO implement
    internal static readonly Option<bool> NoLocalAgent = new("--no-local-agent") {
      Description =
        "Do not host a local agent alongside the coordinator. A remote agent is needed to perform agent tasks."
    };

    internal static readonly Option<bool> NoAgentPort = new("--no-agent-port") {
      Description = "Do not listen for incoming agent connections. Outbound connections are still possible."
    };

    internal static readonly Option<ushort> PortClient = new("--port", "-p") {
      DefaultValueFactory = _ => Ports.AgentDefault - 5,
      Description = "Set the client port (client-to-server communication)."
    };

    internal static readonly Option<ushort> PortAgent = new("--port-agent", "-pa") {
      DefaultValueFactory = _ => Ports.AgentDefault,
      Description = "Set the inbound agent port (agent-to-server communication)."
    };
  }

  internal ServerStartParameters( ParseResult parseResult ) : base( parseResult ) {
    Port = parseResult.GetValue( Options.PortClient );
    PortAgent = parseResult.GetValue( Options.PortAgent );
    NoAgentPort = parseResult.GetValue( Options.NoAgentPort );
  }

  public ushort Port {
    get;
    set;
  }

  public ushort PortAgent {
    get;
    set;
  }

  public bool NoAgentPort {
    get;
    set;
  }
}