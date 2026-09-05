using System.CommandLine;
using Drift.Cli.Abstractions;
using Drift.Cli.Commands.Common.Parameters;

namespace Drift.Cli.Commands.Server.Subcommands.Start;

internal record ServerStartParameters : BaseParameters {
  internal static class Options {
    internal static readonly Option<bool> Daemon = new("--daemon", "-d") {
      Description = "Run the server as a background daemon"
    };

    internal static readonly Option<bool> NoAgent = new("--no-agent") {
      Description = "Do not listen for incoming agent connections. Outbound connections are still possible."
    };

    internal static readonly Option<ushort> PortClient = new("--port", "-p") {
      DefaultValueFactory = _ => Ports.AgentDefault - 5,
      Description =
        "Set the client port (client-to-server communication). Must match the port used by the client / CLI."
    };

    internal static readonly Option<ushort> PortAgent = new("--port-agent", "-pa") {
      DefaultValueFactory = _ => Ports.AgentDefault,
      Description = "Set the agent port (agent-to-server communication). Must match the port used by the agent."
    };
  }

  internal ServerStartParameters( ParseResult parseResult ) : base( parseResult ) {
    PortS = parseResult.GetValue( Options.PortClient );
    PortAgent = parseResult.GetValue( Options.PortAgent );
    NoAgent = parseResult.GetValue( Options.NoAgent );
  }

  public ushort PortS {
    get;
    set;
  }

  public ushort PortAgent {
    get;
    set;
  }

  public bool NoAgent {
    get;
    set;
  }
}