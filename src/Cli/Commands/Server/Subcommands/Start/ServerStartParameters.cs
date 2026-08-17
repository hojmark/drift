using System.CommandLine;
using Drift.Cli.Abstractions;
using Drift.Cli.Commands.Common.Parameters;

namespace Drift.Cli.Commands.Server.Subcommands.Start;

internal record ServerStartParameters : BaseParameters {
  internal static class Options {
    internal static readonly Option<bool> Daemon = new("--daemon", "-d") {
      Description = "Run the server as a background daemon"
    };

    internal static readonly Option<ushort> PortS = new("--port", "-p") {
      DefaultValueFactory = _ => Ports.AgentDefault - 5, Description = "Set the port used for communication"
    };

    internal static readonly Option<ushort> PortAgent = new("--port-agent", "-pa") {
      DefaultValueFactory = _ => Ports.AgentDefault, Description = "Set the port used for agent communication"
    };
  }

  internal ServerStartParameters( ParseResult parseResult ) : base( parseResult ) {
    PortS = parseResult.GetValue( Options.PortS );
    PortAgent = parseResult.GetValue( Options.PortAgent );
  }

  public ushort PortS {
    get;
    set;
  }

  public ushort PortAgent {
    get;
    set;
  }
}