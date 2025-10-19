using System.CommandLine;
using Drift.Cli.Commands.Common;

namespace Drift.Cli.Commands.Preview.Agent.Subcommands;

internal record AgentStartParameters : BaseParameters {
  internal static class Options {
    internal static readonly Option<bool> Adoptable = new("--adoptable") {
      Description = "Allow this agent to be adopted by another peer in the distributed agent network"
    };

    // terminology: agent network or agent group?
    // support @ for supplying local file
    internal static readonly Option<string> Join = new("--join") {
      Description = "Join the distributed agent network using a JWT"
    };

    internal static readonly Option<bool> Daemon = new("--daemon", "-d") {
      Description = "Run the agent as a background daemon"
    };

    internal static readonly Option<uint> Port = new("--port", "-p") {
      DefaultValueFactory = _ => 51515,
      Description = "Set the port used for both adoption and communication. Default is 51515"
    };
  }

  internal AgentStartParameters( ParseResult parseResult ) : base( parseResult ) {
    Port = parseResult.GetValue( Options.Port );
  }

  public uint Port {
    get;
    set;
  }
}